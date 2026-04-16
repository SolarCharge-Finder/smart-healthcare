import '../styles/globals.css';
import Providers from '../components/layout/providers';
import Navbar from '../components/layout/navbar';
import { AuthProvider } from '../modules/auth/AuthContext';

export const metadata = {
  title: 'SmartHealth Appointment System',
  description: 'Healthcare appointment booking dashboard',
};

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="en">
      <body>
        <Providers>
          <AuthProvider>
            <Navbar />
            {children}
          </AuthProvider>
        </Providers>
      </body>
    </html>
  );
}
