import { Navigate, Outlet, useLocation } from 'react-router-dom';
import { Loader2 } from 'lucide-react';
import { useQuery } from '@tanstack/react-query';
import { useCurrentUser } from '@/features/auth/useCurrentUser';
import subscriptionApi from '@/features/subscriptions/api/subscription-api';
import {
  isActiveOrTrialingStatus,
  isExpiredStatus,
  isPastDueStatus,
  isPaymentFailedStatus,
  isSubscribeOnlyStatus,
} from '@/features/subscriptions/subscription-status';

/**
 * SubscriptionGuard wraps protected app routes.
 * - While fetching user data → shows a loading spinner.
 * - If subscription is not active → redirects to /onboarding/subscribe.
 * - Otherwise → renders child routes via <Outlet />.
 */
export default function SubscriptionGuard() {
  const { data: user, isLoading } = useCurrentUser();
  const location = useLocation();
  const shouldResolvePaymentFailureFlow =
    !isLoading &&
    user?.isSubscriptionExist !== false &&
    isPaymentFailedStatus(user?.subscriptionStatus);

  const paymentFailedFlowQuery = useQuery({
    queryKey: ['subscription', 'me', 'payment-failed-flow'],
    queryFn: subscriptionApi.getMySubscription,
    enabled: shouldResolvePaymentFailureFlow,
    retry: false,
  });

  // Show loading spinner while fetching user data
  if (isLoading || (shouldResolvePaymentFailureFlow && paymentFailedFlowQuery.isLoading)) {
    return (
      <div className="flex h-screen w-full items-center justify-center">
        <Loader2 className="h-8 w-8 animate-spin text-primary" />
      </div>
    );
  }
  if (user?.role === 'Admin') {
    return <Outlet />;
  }

  // Prevent redirect loops - if already on an onboarding page, allow access
  const isOnOnboardingPage = location.pathname.startsWith('/onboarding');
  if (isOnOnboardingPage) {
    return <Outlet />;
  }

  console.log('User subscription status:', user?.subscriptionStatus);

  if (user?.isSubscriptionExist === false) {
    return <Navigate to="/onboarding/subscribe" replace />;
  }

  if (isActiveOrTrialingStatus(user?.subscriptionStatus)) {
    return <Outlet />;
  }

  if (isExpiredStatus(user?.subscriptionStatus)) {
    return <Navigate to="/onboarding/renew" replace />;
  }

  // Allow past-due users to continue using the app during grace period.
  if (isPastDueStatus(user?.subscriptionStatus)) {
    return <Outlet />;
  }

  if (isPaymentFailedStatus(user?.subscriptionStatus)) {
    console.log('Resolving payment failure flow, subscription details:', paymentFailedFlowQuery.data);
    if (paymentFailedFlowQuery.data?.previousSubscriptionId) {
        console.log('Previous subscription ID exists, redirecting to renew page');
      return <Navigate to="/onboarding/renew"  />;
    }
    console.log('No previous subscription ID, redirecting to subscribe page');
    return <Navigate to="/onboarding/subscribe" replace />;
  }

  if (isSubscribeOnlyStatus(user?.subscriptionStatus)) {
    return <Navigate to="/onboarding/subscribe" replace />;
  }

  if (!isActiveOrTrialingStatus(user?.subscriptionStatus)) {
    return <Navigate to="/onboarding/subscribe" replace />;
  }


  // All checks passed - render protected content
  return <Outlet />;
}
