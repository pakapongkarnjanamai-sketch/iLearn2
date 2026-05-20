import config from 'devextreme/core/config'
import { appConfig } from './config/appConfig'

if (appConfig.devExtremeLicenseKey) {
  config({ licenseKey: appConfig.devExtremeLicenseKey })
}