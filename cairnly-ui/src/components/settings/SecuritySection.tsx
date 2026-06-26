import { useState } from 'react';
import { useNavigate } from 'react-router';
import { Button, Card, CardContent, CardHeader, Chip, Modal, Separator, Spinner } from '@heroui/react';
import { useAuth } from '../../hooks/useAuth';
import { useDeleteUser, useUnlinkAccount, useUpdatePassword, useUserDetails } from '../../hooks/api';
import { FormField } from '../FormField';
import { ApiErrorDisplay } from '../ApiErrorDisplay';
import { GitHubIcon, GoogleIcon } from '../oauthIcons';
import { showErrorDetails } from '../../utils/environment';
import type { LinkedAccount } from '../../types/models';

/**
 * The Security settings section: password management, linked sign-in providers,
 * and the danger zone (account deletion). Wired to the password update, unlink,
 * and delete-user APIs.
 */
export function SecuritySection() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  const userId = user?.id;
  const { data: details, isLoading } = useUserDetails(userId);

  const updatePassword = useUpdatePassword(userId ?? 0);
  const deleteUser = useDeleteUser(userId ?? 0);
  const unlinkAccount = useUnlinkAccount(userId ?? 0);

  const [passwordOpen, setPasswordOpen] = useState(false);
  const [oldPassword, setOldPassword] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [passwordError, setPasswordError] = useState<Error | null>(null);

  const [deleteOpen, setDeleteOpen] = useState(false);

  if (isLoading || !details) {
    return (
      <div className="flex justify-center py-16">
        <Spinner size="lg" color="accent" />
      </div>
    );
  }

  const handlePasswordSubmit = async () => {
    setPasswordError(null);
    if (newPassword !== confirmPassword) {
      setPasswordError(new Error('New passwords do not match.'));
      return;
    }
    try {
      await updatePassword.mutateAsync({ oldPassword, newPassword });
      setOldPassword('');
      setNewPassword('');
      setConfirmPassword('');
      setPasswordOpen(false);
    } catch (err) {
      setPasswordError(err instanceof Error ? err : new Error('Failed to update password.'));
    }
  };

  const handleDelete = async () => {
    await deleteUser.mutateAsync();
    setDeleteOpen(false);
    await logout();
    navigate('/', { replace: true });
  };

  return (
    <div className="space-y-6">
      {/* Password */}
      <Card className="bg-surface border border-border">
        <CardHeader className="px-6 pt-6">
          <h2 className="text-lg font-semibold">Password</h2>
        </CardHeader>
        <CardContent className="px-6 pb-6 space-y-4">
          <div className="flex items-center justify-between gap-4 flex-wrap">
            <div>
              <p className="font-semibold text-sm">Update Password</p>
              <p className="text-sm text-muted">Change your account password to keep your account secure.</p>
            </div>
            <Modal isOpen={passwordOpen} onOpenChange={setPasswordOpen}>
              <Button variant="outline" onPress={() => setPasswordOpen(true)}>
                Change Password
              </Button>
              <Modal.Backdrop>
                <Modal.Container size="sm">
                  <Modal.Dialog>
                    <Modal.CloseTrigger />
                    <Modal.Header>
                      <Modal.Heading>Change password</Modal.Heading>
                    </Modal.Header>
                    <Modal.Body className="space-y-4">
                      {passwordError && <ApiErrorDisplay error={passwordError} title="Update failed" showDetails={showErrorDetails} />}
                      <FormField label="Current password" type="password" value={oldPassword} onChange={setOldPassword} isRequired autoComplete="current-password" />
                      <FormField label="New password" type="password" value={newPassword} onChange={setNewPassword} isRequired autoComplete="new-password" />
                      <FormField label="Confirm new password" type="password" value={confirmPassword} onChange={setConfirmPassword} isRequired autoComplete="new-password" />
                    </Modal.Body>
                    <Modal.Footer>
                      <Button slot="close" variant="outline">
                        Cancel
                      </Button>
                      <Button onPress={handlePasswordSubmit} isPending={updatePassword.isPending} isDisabled={!oldPassword || !newPassword || !confirmPassword}>
                        Update password
                      </Button>
                    </Modal.Footer>
                  </Modal.Dialog>
                </Modal.Container>
              </Modal.Backdrop>
            </Modal>
          </div>

          <Separator />

          <div className="flex items-center justify-between gap-4 flex-wrap">
            <div>
              <p className="font-semibold text-sm">Reset Password</p>
              <p className="text-sm text-muted">Forgot your password? No problem!</p>
            </div>
            <Button variant="secondary" onPress={() => navigate('/forgot-password')}>
              Reset Password
            </Button>
          </div>
        </CardContent>
      </Card>

      {/* Linked Accounts */}
      <Card className="bg-surface border border-border">
        <CardHeader className="px-6 pt-6">
          <h2 className="text-lg font-semibold">Linked Accounts</h2>
        </CardHeader>
        <CardContent className="px-6 pb-6">
          {details.linkedAccounts.length === 0 ? (
            <p className="text-sm text-muted">No linked accounts.</p>
          ) : (
            <ul className="space-y-3">
              {details.linkedAccounts.map(account => (
                <LinkedAccountRow
                  key={`${account.linkedAccountType}-${account.id}`}
                  account={account}
                  isPending={unlinkAccount.isPending}
                  onUnlink={() => unlinkAccount.mutate(account.linkedAccountType)}
                />
              ))}
            </ul>
          )}
        </CardContent>
      </Card>

      {/* Danger Zone */}
      <Card className="bg-surface border border-danger/40">
        <CardHeader className="px-6 pt-6">
          <h2 className="text-lg font-semibold text-danger">Danger Zone</h2>
        </CardHeader>
        <CardContent className="px-6 pb-6">
          <div className="flex items-center justify-between gap-4 flex-wrap">
            <div>
              <p className="font-semibold text-sm">Delete Account</p>
              <p className="text-sm text-muted">Permanently delete your account and all associated data. This action cannot be undone.</p>
            </div>
            <Modal isOpen={deleteOpen} onOpenChange={setDeleteOpen}>
              <Button variant="danger" onPress={() => setDeleteOpen(true)}>
                Delete Account
              </Button>
              <Modal.Backdrop>
                <Modal.Container size="sm">
                  <Modal.Dialog>
                    <Modal.CloseTrigger />
                    <Modal.Header>
                      <Modal.Heading>Delete account?</Modal.Heading>
                    </Modal.Header>
                    <Modal.Body className="space-y-4">
                      {deleteUser.error && <ApiErrorDisplay error={deleteUser.error as Error} title="Delete failed" showDetails={showErrorDetails} />}
                      <p className="text-sm text-muted">
                        This permanently deletes your account and all associated data. This action cannot be undone.
                      </p>
                    </Modal.Body>
                    <Modal.Footer>
                      <Button slot="close" variant="outline">
                        Cancel
                      </Button>
                      <Button variant="danger" onPress={handleDelete} isPending={deleteUser.isPending}>
                        Delete account
                      </Button>
                    </Modal.Footer>
                  </Modal.Dialog>
                </Modal.Container>
              </Modal.Backdrop>
            </Modal>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}

function LinkedAccountRow({
  account,
  isPending,
  onUnlink
}: {
  account: LinkedAccount;
  isPending: boolean;
  onUnlink: () => void;
}) {
  const [open, setOpen] = useState(false);

  return (
    <li className="flex items-center justify-between gap-4 rounded-lg border border-border bg-surface-secondary px-4 py-3">
      <div className="flex items-center gap-3">
        {account.linkedAccountType === 'Google' ? (
          <GoogleIcon className="size-6" />
        ) : (
          <GitHubIcon className="size-6 text-foreground" />
        )}
        <div>
          <p className="font-semibold text-sm">{account.linkedAccountType}</p>
          <p className="text-xs text-muted">Connected</p>
        </div>
      </div>
      <div className="flex items-center gap-3">
        <Chip variant="soft" size="sm">
          Active
        </Chip>
        <Modal isOpen={open} onOpenChange={setOpen}>
          <Button variant="danger-soft" size="sm" onPress={() => setOpen(true)}>
            Unlink
          </Button>
          <Modal.Backdrop>
            <Modal.Container size="sm">
              <Modal.Dialog>
                <Modal.CloseTrigger />
                <Modal.Header>
                  <Modal.Heading>Unlink {account.linkedAccountType}?</Modal.Heading>
                </Modal.Header>
                <Modal.Body>
                  <p className="text-sm text-muted">
                    You will no longer be able to sign in with {account.linkedAccountType}. You can re-link it later.
                  </p>
                </Modal.Body>
                <Modal.Footer>
                  <Button slot="close" variant="outline">
                    Cancel
                  </Button>
                  <Button
                    variant="danger"
                    isPending={isPending}
                    onPress={() => {
                      onUnlink();
                      setOpen(false);
                    }}
                  >
                    Unlink
                  </Button>
                </Modal.Footer>
              </Modal.Dialog>
            </Modal.Container>
          </Modal.Backdrop>
        </Modal>
      </div>
    </li>
  );
}
