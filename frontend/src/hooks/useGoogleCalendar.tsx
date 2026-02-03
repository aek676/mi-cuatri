import type { ExportSummaryDto, GoogleStatusDto } from '@/lib/api';
import { actions } from 'astro:actions';
import { useCallback, useState } from 'react';
import { toast } from 'sonner';

export function useGoogleCalendar() {
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<Error | null>(null);
  const [isConnected, setIsConnected] = useState<boolean | null>(null);

  const getStatus = useCallback(async (): Promise<GoogleStatusDto> => {
    setLoading(true);
    setError(null);

    try {
      const res = await actions.google.getStatus();
      if (res.error) {
        throw new Error((res.error as any)?.message || 'Error fetching status');
      }
      const data = res.data;
      setIsConnected(data.isConnected);
      return data;
    } catch (err: any) {
      setError(err);
      throw err;
    } finally {
      setLoading(false);
    }
  }, []);

  const connect = useCallback(async (): Promise<{
    url: string;
    stateToken?: string;
  }> => {
    setLoading(true);
    setError(null);

    try {
      const res = await actions.google.connect();
      if (res.error) {
        throw new Error(
          (res.error as any)?.message || 'Error starting connect',
        );
      }
      const data = res.data;
      try {
        await getStatus();
      } catch {
        // ignore status refresh errors
      }
      return data;
    } catch (err: any) {
      setError(err);
      throw err;
    } finally {
      setLoading(false);
    }
  }, [getStatus]);

  const exportEvents = useCallback(
    async (from?: string): Promise<ExportSummaryDto> => {
      setLoading(true);
      setError(null);

      try {
        const res = await actions.google.export({ from });
        if (res.error) {
          throw new Error(
            (res.error as any)?.message || 'Error exporting events',
          );
        }
        const data = res.data as ExportSummaryDto;
        toast.success(
          `Exportado: ${data.created} creados, ${data.updated} actualizados, ${data.failed} fallidos`,
        );
        return data;
      } catch (err: any) {
        setError(err);
        const message = err?.message || 'Error exporting events';
        toast.error(`Error al exportar: ${message}`);
        throw err;
      } finally {
        setLoading(false);
      }
    },
    [],
  );

  return {
    getStatus,
    connect,
    exportEvents,
    loading,
    error,
    isConnected,
  } as const;
}
