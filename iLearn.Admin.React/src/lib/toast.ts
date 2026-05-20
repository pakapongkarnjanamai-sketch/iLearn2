import notify from 'devextreme/ui/notify'

type ToastType = 'success' | 'error' | 'info' | 'warning'

const showToast = (message: string, type: ToastType) => {
  notify(
    {
      message,
      position: {
        at: 'top right',
        my: 'top right',
        offset: '-16 16',
      },
      width: 360,
    },
    type,
    3200,
  )
}

export const toast = {
  success: (message: string) => showToast(message, 'success'),
  error: (message: string) => showToast(message, 'error'),
  info: (message: string) => showToast(message, 'info'),
  warning: (message: string) => showToast(message, 'warning'),
}