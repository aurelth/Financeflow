// Extrai a mensagem de erro de uma resposta da API (Axios) sem depender de `any`.
// Prioridade: erros de validação (todos juntos) > message > fallback amigável.
// `title` (ProblemDetails) fica de fora de propósito: é texto técnico em inglês.
interface ApiErrorBody {
  errors?:  Record<string, string[] | string>
  message?: string
}

export function getApiErrorMessage(err: unknown, fallback: string): string {
  const body = (err as { response?: { data?: ApiErrorBody } } | null | undefined)?.response?.data

  if (body?.errors && Object.keys(body.errors).length > 0)
    return Object.values(body.errors).flat().join(' ')

  return body?.message ?? fallback
}
