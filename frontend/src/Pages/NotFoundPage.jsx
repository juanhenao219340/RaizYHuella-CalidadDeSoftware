import { Link } from 'react-router-dom'

function NotFoundPage() {
  return (
    <main className="flex min-h-dvh flex-col items-center justify-center gap-4 p-4 text-center">
      <h1 className="text-2xl font-bold text-text">Página no encontrada</h1>
      <p>La dirección que busca no existe.</p>
      <Link to="/" className="font-semibold text-primary underline underline-offset-4">
        Volver al inicio
      </Link>
    </main>
  )
}

export default NotFoundPage
