import { useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAuth } from '../lib/AuthContext'
import { FeatureGridSection } from './landing/FeatureGridSection'
import { InsightsShowcaseSection } from './landing/InsightsShowcaseSection'
import { LandingCtaFooter } from './landing/LandingCtaFooter'
import { LandingHero } from './landing/LandingHero'
import { LandingNav } from './landing/LandingNav'
import { TestimonialSection } from './landing/TestimonialSection'

export function LandingPage() {
  const { isAuthenticated, isLoading } = useAuth()
  const navigate = useNavigate()

  useEffect(() => {
    if (!isLoading && isAuthenticated) {
      navigate('/dashboard', { replace: true })
    }
  }, [isLoading, isAuthenticated, navigate])

  return (
    <div className="overflow-x-hidden bg-white">
      <LandingNav />
      <LandingHero />
      <FeatureGridSection />
      <InsightsShowcaseSection />
      <TestimonialSection />
      <LandingCtaFooter />
    </div>
  )
}
