import { Menu } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { cn } from '@/lib/utils';
import NotificationDropdown from '@/features/notifications/NotificationDropdown';
import LanguageSwitcher from '@/components/ui/language-switcher';
import { useEffect } from 'react';
import { useQueryClient } from '@tanstack/react-query';

interface TopNavigationProps {
  onToggleSidebar: () => void;
}

export default function TopNavigation({ onToggleSidebar }: TopNavigationProps) {
const queryClient = useQueryClient();
    useEffect(()=>{
      queryClient.invalidateQueries({ queryKey: ["notifications"] });
    },[queryClient])

  return (
    <nav className={cn(
      'fixed top-0 end-0 z-40 bg-white border-b border-slate-100 h-16 start-0 lg:start-80 transition-all duration-300',
    )}>
      <div className='flex items-center justify-between h-full px-6'>
        {/* Left side - Menu button (mobile/tablet) */}
        <div className='flex items-center'>
          {/* Menu button for mobile/tablet */}
          <Button
            variant="ghost"
            size="icon"
            onClick={onToggleSidebar}
            className='lg:hidden hover:bg-slate-100'
          >
            <Menu className='h-5 w-5 text-slate-700' />
          </Button>
        </div>

        {/* Right Side - Language & Notifications */}
        <div className='flex items-center gap-2'>
          <LanguageSwitcher />
          {/* Notifications */}
          <NotificationDropdown />
        </div>
      </div>
    </nav>
  );
}
