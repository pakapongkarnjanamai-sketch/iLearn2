type ToastType = 'success' | 'error' | 'info' | 'warning'

const SVG_ICONS = {
  success: `<svg class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2.5"><path stroke-linecap="round" stroke-linejoin="round" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" /></svg>`,
  error: `<svg class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2.5"><path stroke-linecap="round" stroke-linejoin="round" d="M10 14l2-2m0 0l2-2m-2 2l-2-2m2 2l2 2m7-2a9 9 0 11-18 0 9 9 0 0118 0z" /></svg>`,
  info: `<svg class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2.5"><path stroke-linecap="round" stroke-linejoin="round" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" /></svg>`,
  warning: `<svg class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2.5"><path stroke-linecap="round" stroke-linejoin="round" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" /></svg>`
}

const TONE_CLASSES = {
  success: {
    iconBg: 'text-emerald-600 bg-emerald-50/70 border border-emerald-100/50',
    progress: 'bg-emerald-500'
  },
  error: {
    iconBg: 'text-rose-600 bg-rose-50/70 border border-rose-100/50',
    progress: 'bg-rose-500'
  },
  info: {
    iconBg: 'text-blue-600 bg-blue-50/70 border border-blue-100/50',
    progress: 'bg-blue-500'
  },
  warning: {
    iconBg: 'text-amber-600 bg-amber-50/70 border border-amber-100/50',
    progress: 'bg-amber-500'
  }
}

const getOrCreateContainer = (): HTMLElement => {
  let container = document.getElementById('app-toast-container')
  if (!container) {
    container = document.createElement('div')
    container.id = 'app-toast-container'
    container.className = 'fixed top-5 right-5 z-[9999] flex flex-col gap-3 w-[360px] max-w-[calc(100vw-40px)] pointer-events-none'
    document.body.appendChild(container)
  }
  return container
}

const showToast = (message: string, type: ToastType) => {
  const container = getOrCreateContainer()

  // Create toast card element
  const card = document.createElement('div')
  card.className = 'relative flex gap-3 p-4 bg-white/95 backdrop-blur-md border border-slate-200/80 rounded-xl shadow-xl transition-all duration-300 ease-out transform translate-x-[400px] opacity-0 pointer-events-auto overflow-hidden'
  
  const tone = TONE_CLASSES[type]

  card.innerHTML = `
    <div class="flex items-center justify-center w-8 h-8 rounded-lg shrink-0 ${tone.iconBg}">
      ${SVG_ICONS[type]}
    </div>
    <div class="flex-1 pr-6 flex items-center min-w-0">
      <p class="text-slate-700 font-semibold text-xs leading-relaxed break-words w-full">${message}</p>
    </div>
    <button class="absolute top-2 right-2 p-1 text-slate-300 hover:text-slate-500 rounded-md hover:bg-slate-50 transition cursor-pointer" aria-label="Close Notification">
      <svg class="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2.5"><path stroke-linecap="round" stroke-linejoin="round" d="M6 18L18 6M6 6l12 12" /></svg>
    </button>
    <div class="absolute bottom-0 left-0 h-0.75 ${tone.progress} w-full transition-all ease-linear duration-3000"></div>
  `

  container.appendChild(card)

  // Trigger animation entry
  requestAnimationFrame(() => {
    setTimeout(() => {
      card.classList.remove('translate-x-[400px]', 'opacity-0')
      card.classList.add('translate-x-0', 'opacity-100')
    }, 10)
  })

  // Start progress bar animation
  const progressBar = card.querySelector('.absolute.bottom-0') as HTMLElement
  if (progressBar) {
    requestAnimationFrame(() => {
      setTimeout(() => {
        progressBar.style.width = '0%'
      }, 50)
    })
  }

  let dismissTimeout: any

  const dismissToast = () => {
    card.classList.remove('translate-x-0', 'opacity-100')
    card.classList.add('translate-x-[400px]', 'opacity-0')
    setTimeout(() => {
      card.remove()
      // Remove container if empty
      const remaining = container.querySelectorAll('.relative')
      if (remaining.length === 0) {
        container.remove()
      }
    }, 300)
  }

  // Bind close button click
  const closeBtn = card.querySelector('button')
  if (closeBtn) {
    closeBtn.addEventListener('click', () => {
      clearTimeout(dismissTimeout)
      dismissToast()
    })
  }

  // Auto-dismiss after 3200ms
  dismissTimeout = setTimeout(dismissToast, 3200)
}

export const toast = {
  success: (message: string) => showToast(message, 'success'),
  error: (message: string) => showToast(message, 'error'),
  info: (message: string) => showToast(message, 'info'),
  warning: (message: string) => showToast(message, 'warning'),
}