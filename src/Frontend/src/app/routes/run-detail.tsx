import { lazy, type Component } from 'solid-js';
import { useNavigate } from '@solidjs/router';

const SessionDetail = lazy(() => import('../../features/IDE/SessionDetail/SessionDetail'));

const SessionDetailPage: Component = () => {
  const navigate = useNavigate();
  const token = localStorage.getItem('accessToken');
  if (!token) {
    navigate('/auth');
    return null;
  }
  return <SessionDetail />;
};

export default SessionDetailPage;
