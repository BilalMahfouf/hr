import { useState } from 'react';
import Sidebar from './SideBar';
import TopNavigation from './TopNavigation';
import { useTranslation } from 'react-i18next';

export default function Layout({ children }: { children: React.ReactNode }) {
  const [sidebarOpen, setSidebarOpen] = useState(false);
  const { i18n } = useTranslation();
  const isRtl = i18n.language === 'ar';

  const toggleSidebar = () => {
    setSidebarOpen(!sidebarOpen);
  };

  const closeSidebar = () => {
    setSidebarOpen(false);
  };

  return (
    <div className='min-h-screen bg-slate-50' dir={isRtl ? 'rtl' : 'ltr'}>
      {/* Sidebar - responsive with state management */}
      <Sidebar isOpen={sidebarOpen} onClose={closeSidebar} />

      {/* Top Navigation */}
      <TopNavigation onToggleSidebar={toggleSidebar} />

      {/* Main Content Area - responsive padding with RTL/LTR support */}
      <main className='pt-16 lg:ps-80 transition-all duration-300'>
        <div className='p-4 md:p-6'>{children}</div>
      </main>
   </div>
  );
}
