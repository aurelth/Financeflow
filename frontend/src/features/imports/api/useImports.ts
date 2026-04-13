import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import axios from 'axios'
import type {
  BankImportDto,
  BankImportPreviewDto,
  ConfirmImportRequestDto,
} from '../types/imports.types'

const API_URL = import.meta.env.VITE_API_URL

function getAuthHeaders() {
  const token = sessionStorage.getItem('accessToken') // Modificado
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
  })
}

// POST /api/imports/ofx
export function useUploadOFX() {
  const queryClient = useQueryClient()
  return useMutation<BankImportDto, Error, File>({
    mutationFn: async (file: File) => {
      const formData = new FormData()
      formData.append('file', file)
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
    },
  })
}