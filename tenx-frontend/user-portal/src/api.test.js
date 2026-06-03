import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest'
import { api } from './api'
import axios from 'axios'
import MockAdapter from 'axios-mock-adapter'

describe('api client', () => {
  let mock

  beforeEach(() => {
    mock = new MockAdapter(api)
    localStorage.clear()
  })

  afterEach(() => {
    mock.restore()
    vi.restoreAllMocks()
  })

  it('adds Authorization header if accessToken exists', async () => {
    localStorage.setItem('accessToken', 'test-token')

    mock.onGet('/test').reply(200)

    const response = await api.get('/test')

    expect(response.config.headers.Authorization).toBe('Bearer test-token')
  })

  it('does not add Authorization header if accessToken is missing', async () => {
    mock.onGet('/test').reply(200)

    const response = await api.get('/test')

    expect(response.config.headers.Authorization).toBeUndefined()
  })

  it('attempts to refresh token on 401 if refreshToken exists', async () => {
    localStorage.setItem('refreshToken', 'test-refresh-token')

    const axiosMock = new MockAdapter(axios)
    // The code does: const { data } = await axios.post...
    // then: localStorage.setItem('accessToken', data.data.accessToken)
    // So the response payload needs to have a 'data' object
    axiosMock.onPost('/api/auth/refresh').reply(200, {
      data: {
        accessToken: 'new-access-token',
        refreshToken: 'new-refresh-token'
      }
    })

    mock.onGet('/test').replyOnce(401)
    mock.onGet('/test').replyOnce(200)

    await api.get('/test')

    expect(localStorage.getItem('accessToken')).toBe('new-access-token')
    expect(localStorage.getItem('refreshToken')).toBe('new-refresh-token')
    axiosMock.restore()
  })

  it('clears localStorage and redirects to login on refresh token failure', async () => {
    localStorage.setItem('refreshToken', 'invalid-refresh-token')
    localStorage.setItem('accessToken', 'old-access-token')

    const axiosMock = new MockAdapter(axios)
    axiosMock.onPost('/api/auth/refresh').reply(401)

    mock.onGet('/test').replyOnce(401)

    // Using vi.stubGlobal to mock location without delete window.location
    const originalLocation = window.location;
    vi.stubGlobal('location', { href: '' });

    try {
      await api.get('/test')
    } catch (err) {
      // Expecting it to potentially throw or just reject
    }

    expect(localStorage.getItem('accessToken')).toBeNull()
    expect(localStorage.getItem('refreshToken')).toBeNull()
    expect(window.location.href).toBe('/login')

    vi.stubGlobal('location', originalLocation);
    axiosMock.restore()
  })
})
