import { createSignal } from 'solid-js';

export function CallScheduler(props) {
  const [title, setTitle] = createSignal('');
  const [scheduledAt, setScheduledAt] = createSignal('');
  const [type, setType] = createSignal('Audio');

  const scheduleCall = async () => {
    // API call to schedule
    console.log('Scheduling call:', { title: title(), scheduledAt: scheduledAt(), type: type() });
  };

  return (
    <div class="call-scheduler">
      <h3>Schedule Call</h3>
      <input placeholder="Title" value={title()} onInput={(e) => setTitle(e.target.value)} />
      <input type="datetime-local" value={scheduledAt()} onInput={(e) => setScheduledAt(e.target.value)} />
      <select value={type()} onChange={(e) => setType(e.target.value)}>
        <option value="Audio">Audio</option>
        <option value="Video">Video</option>
      </select>
      <button onClick={scheduleCall}>Schedule</button>
    </div>
  );
}