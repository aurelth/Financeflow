import axios from 'axios'

const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL,
  withCredentials: true, // envia httpOnly cookie do refresh token
  headers: { 'Content-Type': 'application/json' },
})

// ─── Request interceptor — injeta Access Token ──────────
api.interceptors.request.use(config => {
  const token = sessionStorage.getItem('accessToken')
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})

// ─── Response interceptor — renova token expirado ───────
api.interceptors.response.use(
  res => res,
  async err => {
    const original = err.config

    if (err.response?.status === 401 && !original._retry) {
      original._retry = true

      try {
        const { data } = await axios.post(
          `${import.meta.env.VITE_API_URL}/api/auth/refresh`,
          { accessToken: sessionStorage.getItem('accessToken') },
          { withCredentials: true }
        )
        sessionStorage.setItem('accessToken', data.accessToken)
        original.headers.Authorization = `Bearer ${data.accessToken}`
        return api(original)
      } catch {
        sessionStorage.removeItem('accessToken')
        window.location.href = '/login'
      }
    }

    return Promise.reject(err)
  }
)

export default api