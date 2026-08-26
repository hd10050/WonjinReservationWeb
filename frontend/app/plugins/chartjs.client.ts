// Chart.js 요소를 한 곳에서 등록한다(kpi.vue·stats.vue 양쪽에서 중복 등록하지 않도록, D21).
// Canvas 기반이라 SSR에서 쓸 일이 없으므로 클라이언트 전용 플러그인(.client.ts)으로 둔다.
import {
  Chart as ChartJS,
  Title,
  Tooltip,
  Legend,
  CategoryScale,
  LinearScale,
  BarElement,
  BarController,
  LineElement,
  LineController,
  PointElement,
  ArcElement,
  DoughnutController,
} from 'chart.js'

export default defineNuxtPlugin(() => {
  ChartJS.register(
    Title, Tooltip, Legend,
    CategoryScale, LinearScale,
    BarElement, BarController,
    LineElement, LineController, PointElement,
    ArcElement, DoughnutController,
  )
})
