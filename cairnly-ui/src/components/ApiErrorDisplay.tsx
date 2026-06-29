import { Card, CardContent, CardHeader } from '@heroui/react';
import { AlertCircle } from 'lucide-react';
import { ApiError } from '../types/errors';

interface ApiErrorDisplayProps {
  error: Error | ApiError;
  title?: string;
  className?: string;
  showDetails?: boolean;
}

export function ApiErrorDisplay({ error, title = 'Error', className = '', showDetails = false }: ApiErrorDisplayProps) {
  const isApiError = error instanceof ApiError;

  return (
    <Card className={`border-danger ${className}`}>
      <CardHeader className="pb-3">
        <div className="w-full">
          <h3 className="text-lg font-semibold text-danger flex items-center gap-2">
            <AlertCircle className="size-4 shrink-0" aria-hidden="true" />
            {title}
          </h3>
        </div>
      </CardHeader>
      <CardContent className="pt-0">
        <div className="space-y-3">
          <div className="p-3 bg-danger/10 border border-danger/20 rounded-lg">
            <p className="text-danger text-sm">{isApiError ? error.displayMessage : error.message}</p>
          </div>

          {isApiError && error.allErrors.length > 1 && (
            <div className="space-y-2">
              <h4 className="text-sm font-medium text-danger">Additional Details:</h4>
              {error.allErrors.slice(1).map((errorMsg, index) => (
                <div key={index} className="p-2 bg-danger/5 border border-danger/10 rounded-sm text-sm text-danger">
                  {errorMsg}
                </div>
              ))}
            </div>
          )}

          {showDetails && isApiError && (error.correlationId || error.traceId) && (
            <div className="pt-2 border-t border-danger/20">
              <h4 className="text-xs font-medium text-muted mb-2">Debug Information:</h4>
              <div className="space-y-1 text-xs text-muted">
                {error.status && (
                  <p className="text-sm text-muted mt-1">
                    Status: {error.status} {error.statusText}
                  </p>
                )}
                {error.correlationId && (
                  <p>
                    <strong>Correlation ID:</strong> {error.correlationId}
                  </p>
                )}
                {error.traceId && (
                  <p>
                    <strong>Trace ID:</strong> {error.traceId}
                  </p>
                )}
                {error.instance && (
                  <p>
                    <strong>Request:</strong> {error.instance}
                  </p>
                )}
              </div>
            </div>
          )}
        </div>
      </CardContent>
    </Card>
  );
}
