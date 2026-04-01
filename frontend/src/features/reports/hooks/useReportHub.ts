import { useEffect, useRef } from 'react'
import * as signalR from '@microsoft/signalr'
import { useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'

export function useReportHub() {
  const qc            = useQueryClient()
  const connectionRef = useRef<signalR.HubConnection | null>(null)

  useEffect(() => {
    const connection = new signalR.HubConnectionBuilder()
      .withUrl('https://localhost:7195/hubs/reports', {
        accessTokenFactory: () =>
          sessionStorage.getItem('accessToken') ?? '',
        transport: signalR.HttpTransportType.LongPolling,
      })
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Warning)
      .build()

    connectionRef.current = connection

    connection.on('ReportReady', (data: { reportId: string; fileName: string }) => {
      toast.success(`Relatório "${data.fileName}" está pronto!`, {
        duration: 8000,
        action: {
          label:   'Ver relatórios',
          onClick: () => window.location.href = '/exports',
        },
      })
      qc.invalidateQueries({ queryKey: ['reports'] })
    })

    connection
      .start()
      .then(() => console.log('SignalR conectado'))
      .catch(err => console.warn('SignalR connection failed:', err))

    return () => {
      connection.stop()
    }
  }, [qc])
}