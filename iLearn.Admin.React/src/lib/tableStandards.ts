export const DETAIL_TABLE_CHUNK_SIZE = 100

export function shouldLoadMoreOnScroll(target: HTMLElement, thresholdPx = 60) {
	return target.scrollHeight - target.scrollTop - target.clientHeight <= thresholdPx
}
