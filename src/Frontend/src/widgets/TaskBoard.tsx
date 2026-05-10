import { For } from 'solid-js';

export function TaskBoard(props) {
  return (
    <div class="task-board">
      <h3>Tasks</h3>
      <div class="columns">
        <div class="column">
          <h4>To Do</h4>
          <For each={props.tasks.filter(t => t.status === 'Todo')}>
            {(task) => <div class="task-card">{task.title}</div>}
          </For>
        </div>
        <div class="column">
          <h4>In Progress</h4>
          <For each={props.tasks.filter(t => t.status === 'InProgress')}>
            {(task) => <div class="task-card">{task.title}</div>}
          </For>
        </div>
        <div class="column">
          <h4>Done</h4>
          <For each={props.tasks.filter(t => t.status === 'Done')}>
            {(task) => <div class="task-card">{task.title}</div>}
          </For>
        </div>
      </div>
    </div>
  );
}