import { useState } from 'react';
import { Button, Chip, Label, Spinner } from '@heroui/react';
import { useCreateTag, useTags } from '../../hooks/tags';
import type { Tag } from '../../types/tags';

interface TagSelectorProps {
  /** Currently selected tag IDs. */
  value: number[];
  /** Called with the updated set of selected tag IDs. */
  onChange(ids: number[]): void;
  label?: string;
}

/**
 * Lets the user attach tags to a line item by selecting existing tags or creating
 * new ones inline. Selected tags render as removable chips; typing filters
 * suggestions and offers to create a new tag when no exact match exists.
 */
export function TagSelector({ value, onChange, label = 'Tags' }: TagSelectorProps) {
  const { tags, tagsById, isLoading } = useTags();
  const createTag = useCreateTag();
  const [input, setInput] = useState('');

  const term = input.trim().toLowerCase();
  const selected = value.map(id => tagsById.get(id)).filter((tag): tag is Tag => tag !== undefined);

  const suggestions = tags
    .filter(tag => !value.includes(tag.id) && (term === '' || tag.name.toLowerCase().includes(term)))
    .slice(0, 8);

  const exactMatch = tags.find(tag => tag.name.toLowerCase() === term);

  const addTag = (id: number) => {
    if (!value.includes(id)) {
      onChange([...value, id]);
    }
    setInput('');
  };

  const removeTag = (id: number) => {
    onChange(value.filter(tagId => tagId !== id));
  };

  const createAndAdd = async () => {
    const name = input.trim();

    if (!name) {
      return;
    }

    if (exactMatch) {
      addTag(exactMatch.id);
      return;
    }

    const created = await createTag.mutateAsync({ name });
    onChange([...value, created.id]);
    setInput('');
  };

  const handleKeyDown = (event: React.KeyboardEvent<HTMLInputElement>) => {
    if (event.key === 'Enter') {
      event.preventDefault();
      void createAndAdd();
    } else if (event.key === 'Backspace' && input === '' && value.length > 0) {
      removeTag(value[value.length - 1]);
    }
  };

  return (
    <div className="w-full">
      <Label className="mb-1 block text-sm">{label}</Label>

      <div className="flex flex-wrap items-center gap-2 rounded-lg border border-border bg-surface px-2 py-2">
        {selected.map(tag => (
          <Chip key={tag.id} variant="soft" size="sm">
            {tag.name}
            <button
              type="button"
              aria-label={`Remove ${tag.name}`}
              className="ml-1 text-muted hover:text-foreground"
              onClick={() => removeTag(tag.id)}
            >
              ×
            </button>
          </Chip>
        ))}
        <input
          value={input}
          onChange={event => setInput(event.target.value)}
          onKeyDown={handleKeyDown}
          placeholder={selected.length === 0 ? 'Add tags (e.g. subscription)' : 'Add tag'}
          className="flex-1 min-w-32 bg-transparent text-sm outline-none placeholder:text-muted"
        />
        {(isLoading || createTag.isPending) && <Spinner size="sm" color="accent" />}
      </div>

      {(suggestions.length > 0 || (term !== '' && !exactMatch)) && (
        <div className="mt-2 flex flex-wrap items-center gap-2">
          {suggestions.map(tag => (
            <Button key={tag.id} variant="outline" size="sm" onPress={() => addTag(tag.id)}>
              {tag.name}
            </Button>
          ))}
          {term !== '' && !exactMatch && (
            <Button variant="secondary" size="sm" onPress={() => void createAndAdd()} isPending={createTag.isPending}>
              Create “{input.trim()}”
            </Button>
          )}
        </div>
      )}
    </div>
  );
}
