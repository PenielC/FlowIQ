/**
 * ExchangeRate-API's free terms require a link wherever its rates are shown. Frankfurter (ECB rates) needs none.
 */
export function RateAttribution({ source }: { source: string | null | undefined }) {
  if (source !== 'ExchangeRate-API') return null
  return (
    <p className="mt-1 text-xs text-slate-400">
      <a href="https://www.exchangerate-api.com" target="_blank" rel="noreferrer" className="underline hover:text-slate-600">
        Rates By Exchange Rate API
      </a>
    </p>
  )
}
