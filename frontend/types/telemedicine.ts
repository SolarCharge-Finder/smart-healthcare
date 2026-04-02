export interface TelemedicineSessionResponse {
  appointmentId: string;
  channelName: string;
  patientToken: string;
  doctorToken: string;
  expiresAt: string;
}

export interface CreateSessionRequest {
  appointmentId: string;
}
