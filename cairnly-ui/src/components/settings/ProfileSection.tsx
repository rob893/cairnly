import { useState } from 'react';
import { Button, Card, CardContent, CardHeader, Modal, Spinner } from '@heroui/react';
import { CheckCircle2 } from 'lucide-react';
import { useAuth } from '../../hooks/useAuth';
import { useSendEmailConfirmation, useUpdateUsername, useUserDetails } from '../../hooks/users';
import { FormField } from '../FormField';
import { ApiErrorDisplay } from '../ApiErrorDisplay';
import { showErrorDetails } from '../../utils/environment';
import { showSuccessToast } from '../../utils/notifications';

/** Formats an ISO date string as a localized long date. */
function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString(undefined, { year: 'numeric', month: 'long', day: 'numeric' });
}

function InfoRow({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div>
      <p className="text-xs font-semibold uppercase tracking-wide text-muted mb-1">{label}</p>
      <div className="text-sm">{children}</div>
    </div>
  );
}

/**
 * The Profile settings section: shows the user's identity details (username,
 * email with verification, member since) with inline editing. Wired to the user
 * details, username update, and email confirmation APIs.
 */
export function ProfileSection() {
  const { user } = useAuth();
  const userId = user?.id;
  const { data: details, isLoading } = useUserDetails(userId);

  const updateUsername = useUpdateUsername(userId ?? 0);
  const sendEmailConfirmation = useSendEmailConfirmation(userId ?? 0);

  const [usernameOpen, setUsernameOpen] = useState(false);
  const [newUsername, setNewUsername] = useState('');

  if (isLoading || !details) {
    return (
      <div className="flex justify-center py-16">
        <Spinner size="lg" color="accent" />
      </div>
    );
  }

  const handleUsernameSubmit = async () => {
    if (!newUsername.trim()) {
      return;
    }
    await updateUsername.mutateAsync({ newUsername: newUsername.trim() });
    showSuccessToast('Profile updated');
    setUsernameOpen(false);
  };

  return (
    <Card className="bg-surface border border-border">
      <CardHeader className="px-6 pt-6">
        <h2 className="text-lg font-semibold">Profile</h2>
      </CardHeader>
      <CardContent className="px-6 pb-6 grid grid-cols-1 sm:grid-cols-2 gap-6">
        <InfoRow label="Username">
          <div className="flex items-center gap-2">
            <span className="font-medium">{details.userName}</span>
            <Modal isOpen={usernameOpen} onOpenChange={setUsernameOpen}>
              <Button
                variant="ghost"
                size="sm"
                onPress={() => {
                  setNewUsername(details.userName);
                  setUsernameOpen(true);
                }}
              >
                Edit
              </Button>
              <Modal.Backdrop>
                <Modal.Container size="sm">
                  <Modal.Dialog>
                    <Modal.CloseTrigger />
                    <Modal.Header>
                      <Modal.Heading>Change username</Modal.Heading>
                    </Modal.Header>
                    <Modal.Body className="space-y-4">
                      {updateUsername.error && (
                        <ApiErrorDisplay
                          error={updateUsername.error as Error}
                          title="Update failed"
                          showDetails={showErrorDetails}
                        />
                      )}
                      <FormField
                        label="New username"
                        value={newUsername}
                        onChange={setNewUsername}
                        isRequired
                        autoComplete="username"
                      />
                    </Modal.Body>
                    <Modal.Footer>
                      <Button slot="close" variant="outline">
                        Cancel
                      </Button>
                      <Button
                        onPress={handleUsernameSubmit}
                        isPending={updateUsername.isPending}
                        isDisabled={!newUsername.trim()}
                      >
                        Save
                      </Button>
                    </Modal.Footer>
                  </Modal.Dialog>
                </Modal.Container>
              </Modal.Backdrop>
            </Modal>
          </div>
        </InfoRow>

        <InfoRow label="Email">
          <div className="flex items-center gap-2 flex-wrap">
            <span className="font-medium">{details.email}</span>
            {details.emailConfirmed ? (
              <span className="inline-flex items-center gap-1 text-success text-xs font-medium">
                <CheckCircle2 className="size-4" aria-hidden="true" />
                Verified
              </span>
            ) : (
              <Button
                variant="outline"
                size="sm"
                isPending={sendEmailConfirmation.isPending}
                onPress={() => sendEmailConfirmation.mutate()}
              >
                Verify email
              </Button>
            )}
          </div>
        </InfoRow>

        <InfoRow label="Member Since">
          <span className="font-medium">{formatDate(details.created)}</span>
        </InfoRow>
      </CardContent>
    </Card>
  );
}
