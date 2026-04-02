"use client";

import { Suspense, useEffect, useRef, useState } from "react";
import { useSearchParams, useRouter } from "next/navigation";
import PageHeader from "../../components/ui/PageHeader";
import Card from "../../components/ui/Card";
import Button from "../../components/ui/Button";
import Alert from "../../components/ui/Alert";
import { useCreateTelemedicineSession } from "../../hooks/useTelemedicine";
import { TelemedicineSessionResponse } from "../../types/telemedicine";

// ─── Video Room Component ─────────────────────────────────────────────────────

interface VideoRoomProps {
  session: TelemedicineSessionResponse;
  agoraAppId: string;
}

function VideoRoom({ session, agoraAppId }: VideoRoomProps) {
  const [isMuted, setIsMuted] = useState(false);
  const [isVideoOff, setIsVideoOff] = useState(false);
  const [isConnected, setIsConnected] = useState(false);
  const [isJoining, setIsJoining] = useState(false);
  const [callError, setCallError] = useState<string | null>(null);
  const clientRef = useRef<any>(null);
  const localTracksRef = useRef<any[]>([]);
  const router = useRouter();

  const expiresAt = new Date(session.expiresAt);
  const timeUntilExpiry = Math.max(
    0,
    Math.floor((expiresAt.getTime() - Date.now()) / 1000 / 60)
  );

  const joinRoom = async () => {
    setIsJoining(true);
    setCallError(null);
    try {
      // Dynamically import Agora SDK (client-side only)
      const AgoraRTC = (await import("agora-rtc-sdk-ng")).default;

      const client = AgoraRTC.createClient({ mode: "rtc", codec: "vp8" });
      clientRef.current = client;

      // Handle remote user published (they joined)
      client.on("user-published", async (user: any, mediaType: any) => {
        await client.subscribe(user, mediaType);
        if (mediaType === "video") {
          const remoteVideoTrack = user.videoTrack;
          remoteVideoTrack?.play("remote-video-container");
        }
        if (mediaType === "audio") {
          user.audioTrack?.play();
        }
      });

      // Join the Agora channel with the patient token
      await client.join(agoraAppId, session.channelName, session.patientToken, null);

      // Create and publish local tracks
      const [micTrack, cameraTrack] = await AgoraRTC.createMicrophoneAndCameraTracks();
      localTracksRef.current = [micTrack, cameraTrack];

      cameraTrack.play("local-video-container");
      await client.publish([micTrack, cameraTrack]);
      setIsConnected(true);
    } catch (err: any) {
      console.error("Agora join error:", err);
      setCallError(
        err?.message?.includes("INVALID_VENDOR_KEY") || err?.code === "INVALID_VENDOR_KEY"
          ? "Invalid Agora App ID — check TelemedicineService configuration."
          : err?.message || "Failed to join video session"
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
      console.error("Error ending call:", err);
    }
    router.push("/appointments");
  };

  const toggleMute = async () => {
    const micTrack = localTracksRef.current.find((t: any) => t.trackMediaType === "audio");
    if (micTrack) {
      await micTrack.setEnabled(isMuted);
      setIsMuted(!isMuted);
    }
  };

  const toggleVideo = async () => {
    const cameraTrack = localTracksRef.current.find((t: any) => t.trackMediaType === "video");
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
      <div className="rounded-lg bg-blue-50 border border-blue-100 p-4 flex flex-wrap gap-4 text-sm">
        <div>
          <span className="font-medium text-gray-600">Channel:</span>{" "}
          <code className="text-blue-700 bg-blue-100 px-1 rounded">{session.channelName}</code>
        </div>
        <div>
          <span className="font-medium text-gray-600">Appointment ID:</span>{" "}
          <code className="text-blue-700 bg-blue-100 px-1 rounded text-xs">{session.appointmentId}</code>
        </div>
        <div>
          <span className="font-medium text-gray-600">Session expires in:</span>{" "}
          <span className={timeUntilExpiry < 10 ? "text-red-600 font-bold" : "text-green-600 font-medium"}>
            {timeUntilExpiry} min
          </span>
        </div>
        <div className="flex items-center gap-2">
          <span
            className={`inline-block w-2 h-2 rounded-full ${
              isConnected ? "bg-green-500" : "bg-gray-400"
            }`}
          />
          <span className="text-gray-600">
            {isConnected ? "Live" : "Not connected"}
          </span>
        </div>
      </div>

      {callError && <Alert type="error">{callError}</Alert>}

      {/* Video Grid */}
      <div className="grid gap-4 md:grid-cols-2">
        {/* Local — Patient */}
        <div className="rounded-xl border border-gray-200 bg-gray-900 overflow-hidden relative min-h-[220px]">
          <div
            id="local-video-container"
            className="w-full h-full min-h-[220px]"
          />
          <span className="absolute bottom-2 left-2 bg-black/60 text-white text-xs px-2 py-1 rounded">
            You (Patient)
          </span>
        </div>

        {/* Remote — Doctor */}
        <div className="rounded-xl border border-gray-200 bg-gray-900 overflow-hidden relative min-h-[220px]">
          <div
            id="remote-video-container"
            className="w-full h-full min-h-[220px] flex items-center justify-center"
          >
            {!isConnected && (
              <p className="text-gray-400 text-sm">Waiting for doctor to join...</p>
            )}
          </div>
          <span className="absolute bottom-2 left-2 bg-black/60 text-white text-xs px-2 py-1 rounded">
            Doctor
          </span>
        </div>
      </div>

      {/* Controls */}
      <div className="flex flex-wrap gap-3 mt-4">
        {!isConnected ? (
          <Button
            type="button"
            onClick={joinRoom}
            disabled={isJoining}
          >
            {isJoining ? "Joining..." : "🎥 Join Video Call"}
          </Button>
        ) : (
          <>
            <Button variant="secondary" type="button" onClick={toggleMute}>
              {isMuted ? "🔇 Unmute" : "🔊 Mute"}
            </Button>
            <Button variant="secondary" type="button" onClick={toggleVideo}>
              {isVideoOff ? "📷 Enable Video" : "🚫 Disable Video"}
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
  const [appointmentId, setAppointmentId] = useState("");
  const [session, setSession] = useState<TelemedicineSessionResponse | null>(null);
  const [urlError, setUrlError] = useState<string | null>(null);
  const [sessionError, setSessionError] = useState<string | null>(null);
  const [retryCount, setRetryCount] = useState(0);
  const [hasRequested, setHasRequested] = useState(false);

  const createSession = useCreateTelemedicineSession();

  // Agora App ID — set in TelemedicineService appsettings
  // Frontend needs to know the App ID (not secret) to init AgoraRTC
  const agoraAppId = process.env.NEXT_PUBLIC_AGORA_APP_ID ?? "";

  useEffect(() => {
    const apt = searchParams.get("appointmentId");
    if (!apt) {
      setUrlError("Missing appointmentId in URL. Please go back and complete your payment.");
      return;
    }
    setAppointmentId(apt);
  }, [searchParams]);

  useEffect(() => {
    if (!appointmentId || hasRequested) return;

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
          const message = error instanceof Error ? error.message : "Failed to create telemedicine session.";
          const lowerMessage = message.toLowerCase();
          const shouldRetry =
            lowerMessage.includes("not found") ||
            lowerMessage.includes("paid") ||
            lowerMessage.includes("temporarily unavailable") ||
            lowerMessage.includes("html") ||
            lowerMessage.includes("resource") ||
            lowerMessage.includes("telemedicine service");

          if (shouldRetry && retryCount < 5) {
            setSessionError("Your consultation is being prepared. Retrying in a moment...");
            window.setTimeout(() => {
              setHasRequested(false);
              setRetryCount((current) => current + 1);
            }, 2000);
            return;
          }

          setSessionError(message);
        },
      }
    );
  }, [appointmentId, hasRequested, createSession, retryCount]);

  // ── Render states ──
  if (urlError) {
    return (
      <main className="mx-auto flex min-h-screen max-w-6xl flex-col gap-6 px-6 py-10">
        <PageHeader
          title="Video Consultation"
          subtitle="Connect with your doctor in a secure virtual room."
        />
        <Alert type="error">{urlError}</Alert>
      </main>
    );
  }

  if (createSession.isPending) {
    return (
      <main className="mx-auto flex min-h-screen max-w-6xl flex-col gap-6 px-6 py-10">
        <PageHeader
          title="Video Consultation"
          subtitle="Connect with your doctor in a secure virtual room."
        />
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
    const isNotPaid = sessionError.toLowerCase().includes("paid");
    return (
      <main className="mx-auto flex min-h-screen max-w-6xl flex-col gap-6 px-6 py-10">
        <PageHeader
          title="Video Consultation"
          subtitle="Connect with your doctor in a secure virtual room."
        />
        <Alert type="error">
          {isNotPaid
            ? "⚠️ Your appointment payment is not confirmed yet. Please complete payment before joining."
            : sessionError}
        </Alert>
      </main>
    );
  }

  if (!session) {
    return (
      <main className="mx-auto flex min-h-screen max-w-6xl flex-col gap-6 px-6 py-10">
        <PageHeader
          title="Video Consultation"
          subtitle="Connect with your doctor in a secure virtual room."
        />
        <Alert type="info">Loading session details...</Alert>
      </main>
    );
  }

  return (
    <main className="mx-auto flex min-h-screen max-w-6xl flex-col gap-6 px-6 py-10">
      <PageHeader
        title="Video Consultation"
        subtitle="Connect with your doctor in a secure virtual room."
      />
      <Card title="Live Video Consultation">
        {!agoraAppId && (
          <Alert type="error" >
            ⚠️ NEXT_PUBLIC_AGORA_APP_ID is not set in .env.local. Video will not connect.
          </Alert>
        )}
        <VideoRoom session={session} agoraAppId={agoraAppId} />
      </Card>
    </main>
  );
}

// ─── Page Export ──────────────────────────────────────────────────────────────

export default function ConsultationPage() {
  return (
    <Suspense
      fallback={
        <main className="mx-auto flex min-h-screen max-w-6xl flex-col gap-6 px-6 py-10">
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
