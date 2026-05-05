'use client'

import { useEffect } from 'react'
import { useParams, useRouter } from 'next/navigation'

export default function ChatRedirect() {
  const params = useParams()
  const router = useRouter()

  useEffect(() => {
    router.replace(`/chats?chat=${params.id}`)
  }, [params.id, router])

  return null
}
