import * as SecureStore from 'expo-secure-store'
import { Platform } from 'react-native'

const ACCESS_TOKEN_KEY = 'flowiq_access_token'
const REFRESH_TOKEN_KEY = 'flowiq_refresh_token'

const isWeb = Platform.OS === 'web'

async function getItem(key: string): Promise<string | null> {
  return isWeb ? localStorage.getItem(key) : SecureStore.getItemAsync(key)
}

async function setItem(key: string, value: string): Promise<void> {
  if (isWeb) {
    localStorage.setItem(key, value)
  } else {
    await SecureStore.setItemAsync(key, value)
  }
}

async function deleteItem(key: string): Promise<void> {
  if (isWeb) {
    localStorage.removeItem(key)
  } else {
    await SecureStore.deleteItemAsync(key)
  }
}

export const tokenStorage = {
  getAccessToken: () => getItem(ACCESS_TOKEN_KEY),
  getRefreshToken: () => getItem(REFRESH_TOKEN_KEY),
  setTokens: async (accessToken: string, refreshToken: string) => {
    await Promise.all([setItem(ACCESS_TOKEN_KEY, accessToken), setItem(REFRESH_TOKEN_KEY, refreshToken)])
  },
  clear: async () => {
    await Promise.all([deleteItem(ACCESS_TOKEN_KEY), deleteItem(REFRESH_TOKEN_KEY)])
  },
}
