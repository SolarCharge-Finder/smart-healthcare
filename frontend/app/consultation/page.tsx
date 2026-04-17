'use client';

import { Suspense, useEffect, useRef, useState } from 'react';
import { useSearchParams, useRouter } from 'next/navigation';
import Link from 'next/link';
import { useQuery } from '@tanstack/react-query';
import PageHeader from '../../components/ui/PageHeader';
import Card from '../../components/ui/Card';
import Button from '../../components/ui/Button';
import Alert from '../../components/ui/Alert';
import { useCreateTelemedicineSession } from '../../hooks/useTelemedicine';
import { TelemedicineSessionResponse } from '../../types/telemedicine';
import { Appointment } from '../../types/appointment';
import api from '../../lib/api';
import PaymentSummary from '../../components/booking/PaymentSummary';
import { authStorage } from '../../modules/auth/infra/authStorage';

type JwtPayload = {
  [key: string]: unknown;
};

function parseUserIdFromToken(token: string | null): string | null {
  if (!token) return null;

  try {
    const payload = token.split('.')[1];
    if (!payload) return null;

    const normalized = payload.replace(/-/g, '+').replace(/_/g, '/');

    const padded = normalized.padEnd(Math.ceil(normalized.length / 4) * 4, '=');

    const decoded = JSON.parse(atob(padded)) as JwtPayload;
    const claim = decoded['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'];

    return typeof claim === 'string' ? claim : null;
  } catch {
    return null;
  }
}

// ─── Video Room Component ─────────────────────────────────────────────────────

interface VideoRoomProps {
  session: TelemedicineSessionResponse;
}

type VideoConsultationDoctor = {
  doctorId: string;
  doctorName: string;
  specialization: string;
  hospitalId: string;
  hospitalName: string;
  isVideoConsultation: boolean;
  nextAvailableSlot: string;
  doctorFee: number;
  hospitalFee: number;
  eChannellingFee: number;
  discount: number;
  totalFee: number;
};

function formatConsultationDate(iso: string) {
  if (!iso) return '-';

  const value = new Date(iso);
  if (Number.isNaN(value.getTime())) return '-';

  return value.toLocaleDateString();
}

function formatConsultationTime(iso: string) {
  if (!iso) return '-';

  const value = new Date(iso);
  if (Number.isNaN(value.getTime())) return '-';

  return value.toLocaleTimeString([], { hour: 'numeric', minute: '2-digit', hour12: true });
}

function getTodayLocalDateString() {
  const now = new Date();
  const year = now.getFullYear();
  const month = String(now.getMonth() + 1).padStart(2, '0');
  const day = String(now.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}

function VideoRoom({ session }: VideoRoomProps) {
  const [isMuted, setIsMuted] = useState(false);
  const [isVideoOff, setIsVideoOff] = useState(false);
  const [isConnected, setIsConnected] = useState(false);
  const [isJoining, setIsJoining] = useState(false);
  const [callError, setCallError] = useState<string | null>(null);
  const [mediaWarning, setMediaWarning] = useState<string | null>(null);
  const clientRef = useRef<any>(null);
  const localTracksRef = useRef<any[]>([]);
  const router = useRouter();

  const expiresAt = new Date(session.expiresAt);
  const timeUntilExpiry = Math.max(0, Math.floor((expiresAt.getTime() - Date.now()) / 1000 / 60));

  const joinRoom = async () => {
    setIsJoining(true);
    setCallError(null);
    setMediaWarning(null);
    try {
      // Dynamically import Agora SDK (client-side only)
      const AgoraRTC = (await import('agora-rtc-sdk-ng')).default;

      const client = AgoraRTC.createClient({ mode: 'rtc', codec: 'vp8' });
      clientRef.current = client;

      // Handle remote user published (they joined)
      client.on('user-published', async (user: any, mediaType: any) => {
        await client.subscribe(user, mediaType);
        if (mediaType === 'video') {
          const remoteVideoTrack = user.videoTrack;
          remoteVideoTrack?.play('remote-video-container');
        }
        if (mediaType === 'audio') {
          user.audioTrack?.play();
        }
      });

      // Join the Agora channel with the patient token
      const resolvedAppId = session.agoraAppId || process.env.NEXT_PUBLIC_AGORA_APP_ID || '';

      if (!resolvedAppId) {
        throw new Error('Agora App ID is missing. Please restart the frontend and retry.');
      }

      await client.join(resolvedAppId, session.channelName, session.patientToken, null);

      // Mark the call as connected after the channel join succeeds.
      // Camera/mic access can still fail on locked-down devices, but the user can remain in the room.
      setIsConnected(true);

      // Create and publish local tracks
      try {
        const [micTrack, cameraTrack] = await AgoraRTC.createMicrophoneAndCameraTracks();
        localTracksRef.current = [micTrack, cameraTrack];

        cameraTrack.play('local-video-container');
        await client.publish([micTrack, cameraTrack]);
      } catch (mediaErr: any) {
        console.error('Media device access error:', mediaErr);

        const deniedAccess =
          mediaErr?.name === 'NotAllowedError' ||
          mediaErr?.code === 'PERMISSION_DENIED' ||
          String(mediaErr?.message || '')
            .toLowerCase()
            .includes('permission denied');

        setMediaWarning(
          deniedAccess
            ? 'Camera or microphone access is blocked on this Mac. The call is joined, but local video/audio cannot start until you allow Chrome access in macOS System Settings > Privacy & Security > Camera/Microphone.'
            : mediaErr?.message || 'Joined the room, but local media could not start.',
        );
      }
    } catch (err: any) {
      console.error('Agora join error:', err);
      setCallError(
        err?.message?.includes('INVALID_VENDOR_KEY') || err?.code === 'INVALID_VENDOR_KEY'
          ? 'Invalid Agora App ID — check TelemedicineService configuration.'
          : err?.message || 'Failed to join video session',
      );
    } finally {
      setIsJoining(false);
    }
  };

  const endCall = async () => {
    try {
      // Stop and close local tracks
      for (const track of localTracksRef.current) {
        track.stop();
        track.close();
      }
      localTracksRef.current = [];

      if (clientRef.current) {
        await clientRef.current.leave();
        clientRef.current = null;
      }

      setIsConnected(false);
    } catch (err) {
      console.error('Error ending call:', err);
    }
    router.push('/appointments');
  };

  const toggleMute = async () => {
    const micTrack = localTracksRef.current.find((t: any) => t.trackMediaType === 'audio');
    if (micTrack) {
      await micTrack.setEnabled(isMuted);
      setIsMuted(!isMuted);
    }
  };

  const toggleVideo = async () => {
    const cameraTrack = localTracksRef.current.find((t: any) => t.trackMediaType === 'video');
    if (cameraTrack) {
      await cameraTrack.setEnabled(isVideoOff);
      setIsVideoOff(!isVideoOff);
    }
  };

  // Cleanup on unmount
  useEffect(() => {
    return () => {
      for (const track of localTracksRef.current) {
        track.stop();
        track.close();
      }
      clientRef.current?.leave().catch(() => {});
    };
  }, []);

  return (
    <div className="space-y-4">
      {/* Session Info */}
      <div className="flex flex-wrap gap-4 p-4 text-sm border border-blue-100 rounded-lg bg-blue-50">
        <div>
          <span className="font-medium text-gray-600">Channel:</span>{' '}
          <code className="px-1 text-blue-700 bg-blue-100 rounded">{session.channelName}</code>
        </div>
        <div>
          <span className="font-medium text-gray-600">Appointment ID:</span>{' '}
          <code className="px-1 text-xs text-blue-700 bg-blue-100 rounded">
            {session.appointmentId}
          </code>
        </div>
        <div>
          <span className="font-medium text-gray-600">Session expires in:</span>{' '}
          <span
            className={
              timeUntilExpiry < 10 ? 'text-red-600 font-bold' : 'text-green-600 font-medium'
            }
          >
            {timeUntilExpiry} min
          </span>
        </div>
        <div className="flex items-center gap-2">
          <span
            className={`inline-block w-2 h-2 rounded-full ${
              isConnected ? 'bg-green-500' : 'bg-gray-400'
            }`}
          />
          <span className="text-gray-600">{isConnected ? 'Live' : 'Not connected'}</span>
        </div>
      </div>

      {callError && <Alert type="error">{callError}</Alert>}
      {mediaWarning && <Alert type="info">{mediaWarning}</Alert>}

      {/* Video Grid */}
      <div className="grid gap-4 md:grid-cols-2">
        {/* Local — Patient */}
        <div className="rounded-xl border border-gray-200 bg-gray-900 overflow-hidden relative min-h-[220px]">
          <div id="local-video-container" className="w-full h-full min-h-[220px]" />
          <span className="absolute px-2 py-1 text-xs text-white rounded bottom-2 left-2 bg-black/60">
            You (Patient)
          </span>
        </div>

        {/* Remote — Doctor */}
        <div className="rounded-xl border border-gray-200 bg-gray-900 overflow-hidden relative min-h-[220px]">
          <div
            id="remote-video-container"
            className="w-full h-full min-h-[220px] flex items-center justify-center"
          >
            {!isConnected && <p className="text-sm text-gray-400">Waiting for doctor to join...</p>}
          </div>
          <span className="absolute px-2 py-1 text-xs text-white rounded bottom-2 left-2 bg-black/60">
            Doctor
          </span>
        </div>
      </div>

      {/* Controls */}
      <div className="flex flex-wrap gap-3 mt-4">
        {!isConnected ? (
          <Button type="button" onClick={joinRoom} disabled={isJoining}>
            {isJoining ? 'Joining...' : '🎥 Join Video Call'}
          </Button>
        ) : (
          <>
            <Button variant="secondary" type="button" onClick={toggleMute}>
              {isMuted ? '🔇 Unmute' : '🔊 Mute'}
            </Button>
            <Button variant="secondary" type="button" onClick={toggleVideo}>
              {isVideoOff ? '📷 Enable Video' : '🚫 Disable Video'}
            </Button>
            <Button variant="danger" type="button" onClick={endCall}>
              📵 End Call
            </Button>
          </>
        )}
      </div>
    </div>
  );
}

// ─── Main Consultation Page Content ──────────────────────────────────────────

function ConsultationPageContent() {
  const searchParams = useSearchParams();
  const todayDate = getTodayLocalDateString();
  const guestFallbackName = 'Sachithra Indrachapa';
  const [appointmentId, setAppointmentId] = useState('');
  const [currentUserId, setCurrentUserId] = useState<string | null>(null);
  const [isAuthReady, setIsAuthReady] = useState(false);
  const [session, setSession] = useState<TelemedicineSessionResponse | null>(null);
  const [sessionError, setSessionError] = useState<string | null>(null);
  const [retryCount, setRetryCount] = useState(0);
  const [hasRequested, setHasRequested] = useState(false);

  const createSession = useCreateTelemedicineSession();

  const consultationDoctors = useQuery<VideoConsultationDoctor[]>({
    queryKey: ['video-consultation-doctors'],
    enabled: !appointmentId,
    queryFn: async () => {
      const { data } = await api.get<VideoConsultationDoctor[]>('/doctors');
      return data.filter((doctor) => doctor.isVideoConsultation);
    },
  });

  const appointment = useQuery<Appointment>({
    queryKey: ['appointment', appointmentId],
    enabled: Boolean(appointmentId),
    queryFn: async () => {
      const { data } = await api.get<Appointment>(`/appointments/${appointmentId}`);
      return data;
    },
  });

  const appointmentStatus = appointment.data?.status?.toUpperCase() ?? '';
  const isPaymentSuccessful = appointmentStatus === 'PAID';

  useEffect(() => {
    const token = authStorage.getToken();
    setCurrentUserId(parseUserIdFromToken(token));
    setIsAuthReady(true);
  }, []);

  useEffect(() => {
    const apt = searchParams.get('appointmentId');
    if (!apt) {
      setAppointmentId('');
      return;
    }
    setAppointmentId(apt);
  }, [searchParams]);

  const eligibilityError = (() => {
    if (!appointmentId || !isAuthReady) return null;

    // Do not block the consultation flow if appointment details are temporarily unavailable.
    // Telemedicine service will perform authoritative validation for paid status and session access.
    if (appointment.isError) {
      return null;
    }

    if (!appointment.data) {
      return null;
    }

    const ownerId = appointment.data.userId;
    if (currentUserId && ownerId && ownerId.toLowerCase() !== currentUserId.toLowerCase()) {
      return 'You are not authorized to join this consultation.';
    }

    if ((appointment.data.status ?? '').toUpperCase() !== 'PAID') {
      return 'Your appointment payment is not confirmed yet. Please complete payment before joining.';
    }

    const slotTime = new Date(appointment.data.slotTime);
    if (Number.isNaN(slotTime.getTime())) {
      return 'Consultation time is invalid. Please contact support.';
    }

    const now = new Date();
    const startWindow = new Date(slotTime.getTime() - 30 * 60 * 1000);
    const endWindow = new Date(slotTime.getTime() + 2 * 60 * 60 * 1000);

    if (now < startWindow || now > endWindow) {
      return `Consultation can be joined from ${startWindow.toLocaleString()} to ${endWindow.toLocaleString()}.`;
    }

    return null;
  })();

  useEffect(() => {
    if (
      !appointmentId ||
      hasRequested ||
      !isAuthReady ||
      Boolean(eligibilityError)
    ) {
      return;
    }

    setHasRequested(true);
    setSessionError(null);
    createSession.mutate(
      { appointmentId },
      {
        onSuccess: (data) => {
          setSession(data);
          setSessionError(null);
        },
        onError: (error) => {
          const message =
            error instanceof Error ? error.message : 'Failed to create telemedicine session.';
          const lowerMessage = message.toLowerCase();
          const shouldRetry =
            lowerMessage.includes('not found') ||
            lowerMessage.includes('temporarily unavailable') ||
            lowerMessage.includes('html') ||
            lowerMessage.includes('resource') ||
            lowerMessage.includes('telemedicine service');

          if (shouldRetry && retryCount < 5) {
            setSessionError('Your consultation is being prepared. Retrying in a moment...');
            window.setTimeout(() => {
              setHasRequested(false);
              setRetryCount((current) => current + 1);
            }, 2000);
            return;
          }

          setSessionError(message);
        },
      },
    );
  }, [
    appointmentId,
    hasRequested,
    createSession,
    retryCount,
    isAuthReady,
    eligibilityError,
  ]);

  useEffect(() => {
    if (eligibilityError) {
      setSessionError(eligibilityError);
      setHasRequested(false);
    }
  }, [eligibilityError]);

  // ── Render states ──
  if (!appointmentId) {
    return (
      <main className="flex flex-col max-w-6xl min-h-screen gap-6 px-6 py-10 mx-auto">
        <PageHeader
          title="Video Consultation Doctors"
          subtitle="Doctors available for medicine consultation through secure video call."
        />

        {consultationDoctors.isLoading ? (
          <Alert type="info">Loading video consultation doctors...</Alert>
        ) : null}

        {consultationDoctors.isError ? (
          <Alert type="error">Unable to load video consultation doctors.</Alert>
        ) : null}

        {consultationDoctors.isSuccess && consultationDoctors.data.length === 0 ? (
          <Alert type="info">No doctors currently marked for video consultation.</Alert>
        ) : null}

        <div className="grid gap-4 md:grid-cols-2">
          {(consultationDoctors.data ?? []).map((doctor) => (
            <Link
              key={`${doctor.doctorId}-${doctor.hospitalId}`}
              href={`/doctors/results?doctorName=${encodeURIComponent(doctor.doctorName)}&date=${todayDate}&telemedicine=1`}
              className="block rounded-2xl transition hover:-translate-y-0.5"
            >
              <div className="relative">
                <span
                  className="absolute z-10 inline-flex items-center justify-center w-10 h-10 text-white bg-red-600 rounded-full shadow right-4 top-4"
                  title="Video Consultation"
                >
                  <svg
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    strokeWidth="2"
                    className="w-5 h-5"
                    aria-hidden="true"
                  >
                    <rect x="3" y="6" width="14" height="12" rx="2" />
                    <path d="M17 10l4-2v8l-4-2z" />
                  </svg>
                </span>

                <Card title={doctor.doctorName}>
                  <div className="space-y-2 text-sm text-gray-700">
                    <p>
                      <span className="font-semibold">Doctor ID:</span> {doctor.doctorId}
                    </p>
                    <p>
                      <span className="font-semibold">Specialization:</span> {doctor.specialization}
                    </p>
                    <p>
                      <span className="font-semibold">Date:</span>{' '}
                      {formatConsultationDate(doctor.nextAvailableSlot)}
                    </p>
                    <p>
                      <span className="font-semibold">Time:</span>{' '}
                      {formatConsultationTime(doctor.nextAvailableSlot)}
                    </p>
                    <p>
                      <span className="font-semibold">Doctor Fee:</span> Rs.{' '}
                      {doctor.doctorFee.toFixed(2)}
                    </p>
                    <p>
                      <span className="font-semibold">eChannelling Fee:</span> Rs.{' '}
                      {doctor.eChannellingFee.toFixed(2)}
                    </p>
                    <p>
                      <span className="font-semibold">Total Fee:</span> Rs.{' '}
                      {doctor.totalFee.toFixed(2)}
                    </p>
                    <p className="pt-1 font-medium text-blue-600">View Available Date and Time</p>
                  </div>
                </Card>
              </div>
            </Link>
          ))}
        </div>
      </main>
    );
  }

  if (createSession.isPending) {
    return (
      <main className="flex flex-col max-w-6xl min-h-screen gap-6 px-6 py-10 mx-auto">
        <PageHeader
          title="Video Consultation"
          subtitle="Connect with your doctor in a secure virtual room."
        />

        {isPaymentSuccessful ? (
          <Alert type="success">
            Your payment was successful. Your doctor channeling is confirmed.
          </Alert>
        ) : null}

        {isPaymentSuccessful && appointment.data ? (
          <PaymentSummary
            doctorFee={appointment.data.doctorFee}
            hospitalFee={appointment.data.hospitalFee}
            eChannellingFee={appointment.data.eChannellingFee}
            discount={appointment.data.discount}
            totalFee={appointment.data.totalFee}
          />
        ) : null}

        <div className="flex items-center justify-center min-h-64">
          <div className="text-center">
            <div className="inline-block mb-4">
              <div className="w-8 h-8 border-4 border-blue-200 rounded-full border-t-blue-600 animate-spin" />
            </div>
            <div className="text-gray-600">Generating secure video session...</div>
          </div>
        </div>
      </main>
    );
  }

  if (sessionError) {
    const isNotPaid = sessionError.toLowerCase().includes('paid');
    return (
      <main className="flex flex-col max-w-6xl min-h-screen gap-6 px-6 py-10 mx-auto">
        <PageHeader
          title="Video Consultation"
          subtitle="Connect with your doctor in a secure virtual room."
        />

        {isPaymentSuccessful ? (
          <Alert type="success">
            Your payment was successful. Your doctor channeling is confirmed.
          </Alert>
        ) : null}

        {isPaymentSuccessful && appointment.data ? (
          <PaymentSummary
            doctorFee={appointment.data.doctorFee}
            hospitalFee={appointment.data.hospitalFee}
            eChannellingFee={appointment.data.eChannellingFee}
            discount={appointment.data.discount}
            totalFee={appointment.data.totalFee}
          />
        ) : null}

        <Alert type="error">
          {isNotPaid
            ? '⚠️ Your appointment payment is not confirmed yet. Please complete payment before joining.'
            : sessionError}
        </Alert>
      </main>
    );
  }

  if (!session) {
    return (
      <main className="flex flex-col max-w-6xl min-h-screen gap-6 px-6 py-10 mx-auto">
        <PageHeader
          title="Video Consultation"
          subtitle="Connect with your doctor in a secure virtual room."
        />

        {isPaymentSuccessful ? (
          <Alert type="success">
            Your payment was successful. Your doctor channeling is confirmed.
          </Alert>
        ) : null}

        {isPaymentSuccessful && appointment.data ? (
          <PaymentSummary
            doctorFee={appointment.data.doctorFee}
            hospitalFee={appointment.data.hospitalFee}
            eChannellingFee={appointment.data.eChannellingFee}
            discount={appointment.data.discount}
            totalFee={appointment.data.totalFee}
          />
        ) : null}

        <Alert type="info">Loading session details...</Alert>
      </main>
    );
  }

  return (
    <main className="flex flex-col max-w-6xl min-h-screen gap-6 px-6 py-10 mx-auto">
      <PageHeader
        title="Video Consultation"
        subtitle="Connect with your doctor in a secure virtual room."
      />

      {isPaymentSuccessful ? (
        <Alert type="success">
          Your payment was successful. Your doctor channeling is confirmed.
        </Alert>
      ) : null}

      {isPaymentSuccessful && appointment.data ? (
        <PaymentSummary
          doctorFee={appointment.data.doctorFee}
          hospitalFee={appointment.data.hospitalFee}
          eChannellingFee={appointment.data.eChannellingFee}
          discount={appointment.data.discount}
          totalFee={appointment.data.totalFee}
        />
      ) : null}

      {!currentUserId ? (
        <Alert type="info"> {appointment.data?.guestUser?.fullName ?? guestFallbackName}</Alert>
      ) : null}

      <Card title="Live Video Consultation">
        <VideoRoom session={session} />
      </Card>
    </main>
  );
}

// ─── Page Export ──────────────────────────────────────────────────────────────

export default function ConsultationPage() {
  return (
    <Suspense
      fallback={
        <main className="flex flex-col max-w-6xl min-h-screen gap-6 px-6 py-10 mx-auto">
          <PageHeader
            title="Video Consultation"
            subtitle="Connect with your doctor in a secure virtual room."
          />
          <div className="flex items-center justify-center min-h-64">
            <div className="text-gray-600">Loading...</div>
          </div>
        </main>
      }
    >
      <ConsultationPageContent />
    </Suspense>
  );
}
