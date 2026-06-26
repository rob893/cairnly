import { Card, CardContent, CardHeader, Chip } from '@heroui/react';

interface SettingsStubSectionProps {
  title: string;
  description: string;
}

/**
 * A placeholder settings section for features that are planned but not yet
 * implemented (e.g. Categories, Rules, Tags). Shows a title, a "Coming soon"
 * chip, and a short description.
 */
export function SettingsStubSection({ title, description }: SettingsStubSectionProps) {
  return (
    <Card className="bg-surface border border-border">
      <CardHeader className="px-6 pt-6 flex items-center justify-between gap-3">
        <h2 className="text-lg font-semibold">{title}</h2>
        <Chip variant="soft" size="sm">
          Coming soon
        </Chip>
      </CardHeader>
      <CardContent className="px-6 pb-10 pt-2">
        <p className="text-sm text-muted">{description}</p>
      </CardContent>
    </Card>
  );
}
