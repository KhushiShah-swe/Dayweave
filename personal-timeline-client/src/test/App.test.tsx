import { fireEvent, render, screen, within, waitFor } from '@testing-library/react';
import { expect, it, vi } from 'vitest';
import App from '../App';
import { DEMO_KEY } from '../lib/demo';
import { api } from '../lib/api';

it('lets a visitor create, edit, search, persist, and delete a demo moment without an API', async () => {
  const requests = vi.spyOn(api, 'get');
  const app = render(<App />);
  fireEvent.click(screen.getByRole('button', { name: 'Explore the demo' }));
  await screen.findByRole('heading', { name: 'Your days, connected.' });
  fireEvent.click(screen.getByRole('link', { name: 'Timeline' }));
  fireEvent.click(screen.getByRole('button', { name: 'Add a moment' }));
  let dialog = screen.getByRole('dialog', { name: 'Save a little of today.' });
  fireEvent.change(within(dialog).getByLabelText('Title'), {
    target: { value: 'A meaningful release' },
  });
  fireEvent.change(within(dialog).getByLabelText('Your story'), {
    target: { value: 'Shipped something useful.' },
  });
  fireEvent.click(within(dialog).getByRole('button', { name: 'Save moment' }));
  await screen.findByRole('heading', { name: 'A meaningful release' });
  expect(JSON.parse(localStorage.getItem(DEMO_KEY)!)).toHaveLength(13);
  fireEvent.click(screen.getByRole('button', { name: 'Edit A meaningful release' }));
  dialog = screen.getByRole('dialog', { name: 'Revisit a moment.' });
  fireEvent.change(within(dialog).getByLabelText('Title'), {
    target: { value: 'A better release' },
  });
  fireEvent.click(within(dialog).getByRole('button', { name: 'Save changes' }));
  await screen.findByRole('heading', { name: 'A better release' });
  fireEvent.change(screen.getByLabelText('Search timeline'), {
    target: { value: 'better release' },
  });
  expect(screen.getByText('1 moment in view')).toBeInTheDocument();
  // A fresh mount simulates returning to the same browser session.
  app.unmount();
  render(<App />);
  await screen.findByRole('heading', { name: 'A better release' });
  fireEvent.click(screen.getByRole('button', { name: 'Delete A better release' }));
  fireEvent.click(screen.getByRole('button', { name: 'Keep moment' }));
  expect(screen.getByRole('heading', { name: 'A better release' })).toBeInTheDocument();
  fireEvent.click(screen.getByRole('button', { name: 'Delete A better release' }));
  fireEvent.click(screen.getByRole('button', { name: 'Remove moment' }));
  await waitFor(() =>
    expect(screen.queryByRole('heading', { name: 'A better release' })).not.toBeInTheDocument(),
  );
  expect(JSON.parse(localStorage.getItem(DEMO_KEY)!)).toHaveLength(12);
  expect(requests).not.toHaveBeenCalled();
});

it('labels demo providers as sample data and never starts real sync', async () => {
  const requests = vi.spyOn(api, 'get');
  render(<App />);
  fireEvent.click(screen.getByRole('button', { name: 'Explore the demo' }));
  fireEvent.click(screen.getByRole('link', { name: 'Connections' }));
  expect(screen.getAllByText('Sample data')).toHaveLength(3);
  fireEvent.click(screen.getAllByRole('button', { name: 'Explore connection' })[0]);
  expect(await screen.findByRole('status')).toHaveTextContent('sample connections');
  expect(requests).not.toHaveBeenCalled();
});

it('keeps synced activity read-only and clears filters', async () => {
  render(<App />);
  fireEvent.click(screen.getByRole('button', { name: 'Explore the demo' }));
  fireEvent.click(screen.getByRole('link', { name: 'Timeline' }));
  await screen.findByText('12 moments in view');
  fireEvent.click(screen.getByRole('button', { name: 'GitHub' }));
  expect(screen.getByText('4 moments in view')).toBeInTheDocument();
  expect(screen.queryByRole('button', { name: /^Edit / })).not.toBeInTheDocument();
  fireEvent.change(screen.getByLabelText('Search timeline'), {
    target: { value: 'does not exist' },
  });
  expect(screen.getByRole('heading', { name: 'No moments found.' })).toBeInTheDocument();
  fireEvent.click(screen.getByRole('button', { name: 'Clear filters' }));
  expect(screen.getByText('12 moments in view')).toBeInTheDocument();
});
