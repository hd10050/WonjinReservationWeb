#!/bin/bash
# 상태 전이 동시성 재현 스크립트 (design.md 10장 · 19-1절)
# 같은 예약에 동일한 상태 전이(Confirmed -> Visited) 요청을 20건 동시에 보내,
# "조회 -> 판단 -> 저장" 3단계였다면 발생했을 상태 뒤집힘/중복 처리가 없는지 검증한다.
# 사용법: 워크트리 루트에서 실행. API_BASE 환경변수로 포트 지정 가능(기본 5201).
set -euo pipefail
cd "$(dirname "$0")/../.."

API_BASE="${API_BASE:-http://localhost:5201}"
COOKIE_JAR=$(mktemp)

curl -s -c "$COOKIE_JAR" -X POST "$API_BASE/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"email":"test-admin@wonjin.local","password":"TestPassword123!"}' > /dev/null

# 매 실행마다 재현 가능하도록 테스트 전용 예약(code=202608260005)을 Confirmed로 리셋
docker compose exec -T postgres psql -U wonjin -d wonjin -c \
  "UPDATE wonjin.reservations SET status='Confirmed', visited_at=NULL, updated_at=now()
   WHERE code = '202608260005';
   DELETE FROM wonjin.reservation_logs WHERE reservation_id = (SELECT id FROM wonjin.reservations WHERE code='202608260005') AND action='status_changed' AND note='Confirmed → Visited';" > /dev/null

RES_ID=$(docker compose exec -T postgres psql -U wonjin -d wonjin -t -A -c \
  "SELECT id FROM wonjin.reservations WHERE code = '202608260005';" | tr -d '\r')

echo "대상 예약 id=$RES_ID, 20건 동시 상태전이(Visited) 요청 전송..."
run_one() {
  curl -s -o /dev/null -w "%{http_code}\n" -b "$1" -X POST "$2/api/admin/reservations/$3/status" \
    -H "Content-Type: application/json" -d '{"status":"Visited"}'
}
export -f run_one

CODES=$(seq 1 20 | xargs -P 20 -I{} bash -c "run_one '$COOKIE_JAR' '$API_BASE' '$RES_ID'")

echo "응답 코드 분포:"
echo "$CODES" | sort | uniq -c

SUCCESS_COUNT=$(echo "$CODES" | grep -c '^200$' || true)
CONFLICT_COUNT=$(echo "$CODES" | grep -c '^409$' || true)

FINAL_STATUS=$(docker compose exec -T postgres psql -U wonjin -d wonjin -t -A -c \
  "SELECT status FROM wonjin.reservations WHERE id=$RES_ID;" | tr -d '\r')
LOG_COUNT=$(docker compose exec -T postgres psql -U wonjin -d wonjin -t -A -c \
  "SELECT COUNT(*) FROM wonjin.reservation_logs WHERE reservation_id=$RES_ID AND action='status_changed' AND note='Confirmed → Visited';" | tr -d '\r')

echo "성공(200): $SUCCESS_COUNT / 충돌(409): $CONFLICT_COUNT / 최종상태: $FINAL_STATUS / 업무이력 기록건수: $LOG_COUNT"

if [ "$SUCCESS_COUNT" -eq 1 ] && [ "$CONFLICT_COUNT" -eq 19 ] && [ "$FINAL_STATUS" = "Visited" ] && [ "$LOG_COUNT" -eq 1 ]; then
  echo "PASS: 20건 중 정확히 1건만 성공, 나머지는 409로 거부. 최종 상태·업무이력 모두 정확히 1회만 반영됨"
else
  echo "FAIL: 예상과 다름 (2건 이상 성공했거나 이력이 중복/누락됨)"
  rm -f "$COOKIE_JAR"
  exit 1
fi

rm -f "$COOKIE_JAR"
