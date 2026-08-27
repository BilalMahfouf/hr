import { createBrowserRouter, Navigate } from 'react-router-dom';
import MainLayout from './common/layouts/MainLayout';
import Login from './features/auth/pages/Login';
import DashboardPage from './features/dashboard/DashboardPage';
import SettingPage from './features/settings/SettingPage';
import RegisterPage from './features/auth/pages/RegisterPage';
import ForgotPasswordPage from './features/auth/pages/ForgotPasswordPage';
import ResetPasswordPage from './features/auth/pages/ResetPasswordPage';
import AuthGuard from './features/auth/AuthGuard';
import RoleGuard from './features/auth/RoleGuard';
import UserPage from './features/users/UserPage';
import SubscribePage from './features/subscriptions/pages/SubscribePage';
import RenewPage from './features/subscriptions/pages/RenewPage';
import PaymentSuccessPage from './features/subscriptions/pages/PaymentSuccessPage';
import PaymentFailedPage from './features/subscriptions/pages/PaymentFailedPage';
import SubscriptionPlansPage from './features/subscriptions/pages/SubscriptionPlansPage';
import MachinesPage from './features/machines/MachinesPage';
import CreateMachinePage from './features/machines/CreateMachinePage';
import EditMachinePage from './features/machines/EditMachinePage';
import ViewMachinePage from './features/machines/ViewMachinePage';
import PunchesPage from './features/attendance/punches-page';
import AttendanceRecordsPage from './features/attendance/attendance-records-page';
import ViewPunchPage from './features/attendance/ViewPunchPage';
import ViewAttendanceRecordPage from './features/attendance/ViewAttendanceRecordPage';
import EmployeesPage from './features/employees/employees-page';
import ViewEmployeePage from './features/employees/view-employee-page';
import EmployeeGroupsPage from './features/employees/employee-groups-page';
import CreateEmployeeGroupPage from './features/employees/create-employee-group-page';
import EditEmployeeGroupPage from './features/employees/edit-employee-group-page';

export const router = createBrowserRouter([
    {
        path: '/',
        element: <Navigate to="/login" replace />,
    },
    {
        path: '/login',
        element: <Login />,
    },
    {
        path: '/register',
        element: <RegisterPage />,
    },
    {
        path: '/forgot-password',
        element: <ForgotPasswordPage />,
    },
    {
        path: '/reset-password',
        element: <ResetPasswordPage />,
    },
    {
        element: <AuthGuard />,
        children: [
            // Onboarding routes (authenticated but no subscription check)
            {
                path: '/onboarding/subscribe',
                element: <SubscribePage />,
            },
            {
                path: '/onboarding/renew',
                element: <RenewPage />,
            },
            // Payment callback routes
            {
                path: '/payment/success',
                element: <PaymentSuccessPage />,
            },
            {
                path: '/payment/failed',
                element: <PaymentFailedPage />,
            },
            // Protected app routes (authenticated + subscription check)
            {
               element: <MainLayout />,
    children: [
        {
            path: '/dashboard',
            element: <DashboardPage />,
        },
        {
            path: '/users',
            element: (
                <RoleGuard requiredRole="admin">
                    <UserPage />
                </RoleGuard>
            ),
        },
        {
            path: '/subscription-plans',
            element: (
                <RoleGuard requiredRole="admin">
                    <SubscriptionPlansPage />
                </RoleGuard>
            ),
        },
        {
            path: '/machines',
            element: (
                <RoleGuard requiredRole="admin">
                    <MachinesPage />
                </RoleGuard>
            ),
        },
        {
            path: '/machines/create',
            element: (
                <RoleGuard requiredRole="admin">
                    <CreateMachinePage />
                </RoleGuard>
            ),
        },
        {
            path: '/machines/:machineId/edit',
            element: (
                <RoleGuard requiredRole="admin">
                    <EditMachinePage />
                </RoleGuard>
            ),
        },
        {
            path: '/machines/:machineId',
            element: (
                <RoleGuard requiredRole="admin">
                    <ViewMachinePage />
                </RoleGuard>
            ),
        },
        {
            path: '/attendance',
            element: <Navigate to="/attendance/punches" replace />,
        },
        {
            path: '/attendance/punches',
            element: <PunchesPage />,
        },
        {
            path: '/attendance/punches/:punchId',
            element: <ViewPunchPage />,
        },
        {
            path: '/attendance/records',
            element: <AttendanceRecordsPage />,
        },
        {
            path: '/attendance/records/:attendanceRecordId',
            element: <ViewAttendanceRecordPage />,
        },
        {
            path: '/employees',
            element: <EmployeesPage />,
        },
        {
            path: '/employees/:id',
            element: <ViewEmployeePage />,
        },
        {
            path: '/employee-groups',
            element: <EmployeeGroupsPage />,
        },
        {
            path: '/employee-groups/new',
            element: <CreateEmployeeGroupPage />,
        },
        {
            path: '/employee-groups/:id',
            element: <EditEmployeeGroupPage />,
        },
        {
            path: '/settings',
            element: <SettingPage />,
        },
    ], 
            },
        ],
    },
    {
        path: '*',
        element: <Navigate to="/login" replace />,
    },
]);
