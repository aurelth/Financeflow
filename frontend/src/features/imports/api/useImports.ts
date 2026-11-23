import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import axios from 'axios'
import type {
  BankImportDto,
  BankImportPreviewDto,
  ConfirmImportRequestDto,
} from '../types/imports.types'

const API_URL = import.meta.env.VITE_API_URL

function getAuthHeaders() {
  const token = sessionStorage.getItem('accessToken')
  return { Authorization: `Bearer ${token}` }
}

// GET /api/imports
export function useImports() {
  return useQuery<BankImportDto[]>({
    queryKey: ['imports'],
    queryFn: async () => {
      const { data } = await axios.get(`${API_URL}/api/imports`, {
        headers: getAuthHeaders(),
      })
      return data
    },
  })
}

// GET /api/imports/{id}/preview
export function useImportPreview(id: string | null) {
  return useQuery<BankImportPreviewDto>({
    queryKey: ['imports', 'preview', id],
    queryFn: async () => {
      const { data } = await axios.get(`${API_URL}/api/imports/${id}/preview`, {
        headers: getAuthHeaders(),
      })
      return data
    },
    enabled: !!id,
    // O Worker processa o arquivo (detecção de duplicatas) logo após o upload
    refetchInterval: (query) => {
      const status = query.state.data?.status
      return status === 'Pending' || status === 'Processing' ? 2000 : false
    },
  })
}

// PUT /api/imports/{id}/account — troca a conta e refaz a detecção de duplicatas
export function useChangeImportAccount(id: string) {
  const queryClient = useQueryClient()
  return useMutation<BankImportDto, Error, string>({
    mutationFn: async (accountId) => {
      const { data } = await axios.put(
        `${API_URL}/api/imports/${id}/account`,
        { accountId },
        { headers: getAuthHeaders() }
      )
      return data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['imports'] })
      // A conta pode ter passado a reconhecer o arquivo (chave do banco aprendida)
      queryClient.invalidateQueries({ queryKey: ['accounts'] })
    },
  })
}

export interface UploadOFXVariables {
  file: File
  accountId?: string
}

// POST /api/imports/ofx
export function useUploadOFX() {
  const queryClient = useQueryClient()
  return useMutation<BankImportDto, Error, UploadOFXVariables>({
    mutationFn: async ({ file, accountId }) => {
      const formData = new FormData()
      formData.append('file', file)
      // Sem accountId o back-end reconhece a conta pelo OFX ou usa a conta principal
      if (accountId) formData.append('accountId', accountId)
      const { data } = await axios.post(`${API_URL}/api/imports/ofx`, formData, {
        headers: {
          ...getAuthHeaders(),
          'Content-Type': 'multipart/form-data',
        },
      })
      return data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['imports'] })
      // O backend pode ter vinculado a chave do banco à conta escolhida
      queryClient.invalidateQueries({ queryKey: ['accounts'] })
    },
  })
}

// POST /api/imports/{id}/confirm
export function useConfirmImport(id: string) {
  const queryClient = useQueryClient()
  return useMutation<BankImportDto, Error, ConfirmImportRequestDto>({
    mutationFn: async (body) => {
      const { data } = await axios.post(
        `${API_URL}/api/imports/${id}/confirm`,
        body,
        { headers: getAuthHeaders() }
      )
      return data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['imports'] })
      queryClient.invalidateQueries({ queryKey: ['accounts'] })
      queryClient.invalidateQueries({ queryKey: ['transactions'] })
      queryClient.invalidateQueries({ queryKey: ['dashboard'] })
    },
  })
}

export function useDeleteImport() {
  const queryClient = useQueryClient()
  return useMutation<void, Error, string>({
    mutationFn: async (id: string) => {
      await axios.delete(`${API_URL}/api/imports/${id}`, {
        headers: getAuthHeaders(),
      })
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['imports'] })
    },
  })
}