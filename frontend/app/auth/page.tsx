'use client';

import { useState } from 'react';
import LoginForm from '../../components/forms/LoginForm';
import RegisterForm from '../../components/forms/RegisterForm';

export default function AuthPage() {
  const [activeTab, setActiveTab] = useState<'login' | 'register'>('login');

  return (
    <main className="mx-auto flex min-h-screen max-w-6xl flex-col gap-8 px-6 py-10">
      <div className="flex flex-1 items-start justify-center">
        <div className="w-full max-w-lg rounded-2xl border border-gray-200 bg-white p-6 shadow-sm">
          <div className="flex rounded-xl border border-gray-200 bg-gray-50 p-1 text-sm">
            <button
              type="button"
              onClick={() => setActiveTab('login')}
              className={`flex-1 rounded-lg px-4 py-2 font-semibold transition ${
                activeTab === 'login'
                  ? 'bg-white text-blue-700 shadow'
                  : 'text-gray-500 hover:text-blue-600'
              }`}
            >
              Login
            </button>
            <button
              type="button"
              onClick={() => setActiveTab('register')}
              className={`flex-1 rounded-lg px-4 py-2 font-semibold transition ${
                activeTab === 'register'
                  ? 'bg-white text-blue-700 shadow'
                  : 'text-gray-500 hover:text-blue-600'
              }`}
            >
              Register
            </button>
          </div>

          <div className="mt-6">{activeTab === 'login' ? <LoginForm /> : <RegisterForm />}</div>

          <p className="mt-6 text-center text-xs text-gray-400">
            All fields are required. Your details are stored securely.
          </p>
        </div>
      </div>
    </main>
  );
}
