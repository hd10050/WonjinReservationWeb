import tailwindcss from '@tailwindcss/vite'

// https://nuxt.com/docs/api/configuration/nuxt-config
export default defineNuxtConfig({
  compatibilityDate: '2025-07-15',
  devtools: { enabled: true },

  // auth-pattern-reference.md 8장 — 재배포 후 열린 탭이 구 JS 청크 404를 맞으면 자동 새로고침.
  // 없으면 auth 플러그인이 실행 안 돼 "로그인이 풀린 것처럼" 보임(버그#4). Cloudflare Workers는
  // 재배포마다 청크 해시가 바뀌는 구조라 실제 발동 가능(design.md TODO, 2026-08-27 반영).
  experimental: {
    emitRouteChunkError: 'reload',
  },

  css: ['~/assets/css/main.css'],
  vite: {
    plugins: [tailwindcss()],
  },

  app: {
    head: {
      link: [
        // M10 — favicon.png(32x32) 명시 등록. 기존 favicon.ico는 그대로 두되(구형 브라우저 폴백),
        // 이 <link>가 있으면 최신 브라우저는 이쪽을 우선한다.
        { rel: 'icon', type: 'image/png', href: '/favicon.png' },
        // Phase 9 — apple-touch-icon도 같은 자산 재사용(권장 180x180에는 못 미치지만 M10 범위 내 자산).
        { rel: 'apple-touch-icon', href: '/favicon.png' },
      ],
      script: [
        {
          // 5-3절 — detectBrowserLanguage(5-2절)를 끈 대신, 자동 감지는 <head> 동기 인라인
          // 스크립트로만 구현한다. 크롤러는 스크립트를 실행하지 않으므로 '/'는 항상 SSR
          // 그대로(zh-CN) 응답된다 — 카카오톡·라인 등 링크 미리보기 봇이 리다이렉트를 따라가
          // 엉뚱한 언어의 og:description을 노출하는 문제를 피하기 위함.
          innerHTML: `(function () {
  try {
    if (location.pathname !== '/') return;
    var manual = /(?:^|; )wj_lang_manual=1(?:;|$)/.test(document.cookie);
    var t;
    if (manual) {
      var m = document.cookie.match(/(?:^|; )wj_lang=([^;]*)/);
      t = m ? decodeURIComponent(m[1]) : 'zh-CN';
    } else {
      var l = navigator.language || '';
      if (l.indexOf('ko') === 0) t = 'ko';
      else if (l === 'zh-TW' || l === 'zh-HK' || l === 'zh-Hant' || l.indexOf('zh-Hant') === 0) t = 'zh-TW';
      else if (l.indexOf('zh') === 0) t = 'zh-CN';
      else t = 'en';
      document.cookie = 'wj_lang=' + t + '; expires=' + new Date(Date.now() + 31536000000).toUTCString() + '; path=/; samesite=lax';
    }
    if (t !== 'zh-CN') location.replace('/' + (t === 'zh-TW' ? 'zh-tw' : t) + location.search);
  } catch (e) {}
})();`,
        },
      ],
    },
  },

  modules: ['@nuxtjs/i18n', '@nuxtjs/sitemap', '@nuxtjs/robots', 'shadcn-nuxt'],

  nitro: {
    preset: 'cloudflare_module',
  },

  // D19 — shadcn-vue. 컴포넌트는 `npx shadcn-vue add <name>`으로 소스를 직접 복사해 여기 쌓인다.
  shadcn: {
    prefix: '',
    componentDir: './app/components/ui',
  },

  runtimeConfig: {
    // 서버 전용 — SSR→백엔드 직접 호출, 랜딩 방문 기록 내부 시크릿(4-3절)
    apiBaseInternal: process.env.NUXT_API_BASE_INTERNAL || '',
    internalSecret: process.env.NUXT_INTERNAL_SECRET || '',
    public: {
      // 동일 출처 프록시(D7) — 반드시 빈 문자열(동일 출처).
      // 🔴 ??로만 폴백할 것 — ||를 쓰면 ""가 falsy로 걸려 폴백 URL로 되돌아가 프록시가 무력화된다(4-1절).
      apiBase: process.env.NUXT_PUBLIC_API_BASE ?? '',
      siteUrl: process.env.NUXT_PUBLIC_SITE_URL || 'https://example.com',
    },
  },

  i18n: {
    // 🔴 Phase 9 보안감사 실측 발견 — 없으면 "I18n baseUrl is required to generate valid SEO
    // tag links" 경고와 함께 useLocaleHead()의 hreflang alternate가 상대경로(예: href="/ko")로
    // 생성된다. SEO 표준은 hreflang이 절대 URL이어야 하므로 반드시 지정할 것(5-1절).
    baseUrl: process.env.NUXT_PUBLIC_SITE_URL || 'https://example.com',
    strategy: 'prefix_except_default',
    defaultLocale: 'zh-CN',
    locales: [
      { code: 'zh-CN', language: 'zh-CN', name: '简体中文', file: 'zh-CN.json' },
      { code: 'zh-TW', language: 'zh-TW', name: '繁體中文', file: 'zh-TW.json' },
      { code: 'en', language: 'en-US', name: 'English', file: 'en.json' },
      { code: 'ko', language: 'ko-KR', name: '한국어', file: 'ko.json' },
    ],
    lazy: true,
    // 🔴 카카오톡·라인 등 링크 미리보기 봇이 언어감지 리다이렉트를 따라가 엉뚱한 언어의
    // og:description을 노출하는 문제 때문에 반드시 false(5-2절). 자동감지는 Phase 2에서
    // <head> 동기 인라인 스크립트로만 구현한다(5-3절) — 서버 302 리다이렉트 방식 금지.
    detectBrowserLanguage: false,
  },

  sitemap: {
    hostname: process.env.NUXT_PUBLIC_SITE_URL,
    // 공개 상세 페이지가 없는 구조(랜딩+privacy뿐)라 동적 URL 소스 불필요 — 파일기반 라우트를
    // @nuxtjs/sitemap이 자동 포함한다(Phase 9 보안감사 시 확인).
    exclude: ['/admin/**'],
  },

  robots: {
    // 🔴 트레일링 슬래시 필수 — 없으면 prefix 매칭으로 무관한 경로까지 막히고
    // sitemap exclude에도 같은 규칙이 적용돼 동적 URL이 원인불명으로 누락된다(5-5절)
    groups: [{ userAgent: ['*'], allow: ['/'], disallow: ['/admin/'] }],
    // 🔴 Phase 9 실측 발견 — sitemap 필드를 직접 지정하면 안 됨. @nuxtjs/sitemap이 설치돼 있으면
    // sitemap_index.xml을 robots.txt에 이미 자동 등록한다(로컬 프로덕션모킹 curl로 확인). 여기서
    // sitemap.xml(메타리프레시 HTML일 뿐, 실제 sitemap 아님)을 직접 추가하면 design.md 5-5절이
    // 경고하는 "잘못된 sitemap 제출" 상황이 robots.txt 자체에 중복으로 생긴다.
  },
})
