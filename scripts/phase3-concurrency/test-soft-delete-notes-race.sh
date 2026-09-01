#!/bin/bash
# 취소·상담기록 동시성 재현 스크립트 (design.md 11-2절 · 19-1절)
# 🔴 2026-09-01 재작성 — 원래는 소프트 삭제(D15) 조건("상담 기록 0건일 때만 삭제 가능") 경합을
# 검증했으나, D24(2026-08-27)로 소프트 삭제 자체가 폐지되고 DELETE 엔드포인트가 삭제되어 원본
# 스크립트는 전부 404만 받는 죽은 스크립트가 됐다(감사에서 발견). AddNote의 자체 가드 코드 주석이
# 이미 "경쟁 대상이 원래 SoftDelete/D15였으나 지금은 예약 취소(Cancelled 전이)가 그 자리를
# 대신한다"고 명시하고 있어, 그 취소-vs-상담기록추가 경합으로 재작성한다.
# 취소된 예약도 상담 기록은 남아있는 게 정상(계약 위반 아님)이라 "취소됐는데 기록이 있으면 FAIL"
# 같은 상호배타 조건은 성립하지 않는다 — 대신 검증할 불변식은 "성공(200) 응답 건수만큼 정확히
# DB에 저장돼야 한다"(row-lock 기반 ExecuteUpdateAsync가 응답과 실제 저장을 항상 일치시키는지).
# 사용법: 워크트리 루트에서 실행. API_BASE 환경변수로 포트 지정 가능(기본 5201).
set -euo pipefail
cd "$(dirname "$0")/../.."

API_BASE="${API_BASE:-http://localhost:5201}"
COOKIE_JAR=$(mktemp)

curl -s -c "$COOKIE_JAR" -X POST "$API_BASE/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"email":"test-admin@wonjin.local","password":"TestPassword123!"}' > /dev/null

RES_ID=$(docker compose exec -T postgres psql -U wonjin -d wonjin -t -A -c \
  "SELECT id FROM wonjin.reservations WHERE code = '202608260006';" | tr -d '\r')

# 매 실행마다 재현 가능하도록 리셋 — 상담 기록 전부 삭제 + Confirmed로 원복(취소 가능한 상태)
docker compose exec -T postgres psql -U wonjin -d wonjin -c \
  "DELETE FROM wonjin.reservation_notes WHERE reservation_id = $RES_ID;
   UPDATE wonjin.reservations SET status='Confirmed', cancelled_at = NULL, cancel_reason = NULL WHERE id = $RES_ID;" > /dev/null

echo "대상 예약 id=$RES_ID, 취소 10건 + 상담기록추가 10건 동시 전송..."

run_cancel() {
  curl -s -o /dev/null -w "CANCEL:%{http_code}\n" -b "$1" -X POST "$2/api/admin/reservations/$3/status" \
    -H "Content-Type: application/json" -d '{"status":"Cancelled","cancelReason":"race condition test"}'
}
run_note() {
  curl -s -o /dev/null -w "NOTE:%{http_code}\n" -b "$1" -X POST "$2/api/admin/reservations/$3/notes" \
    -H "Content-Type: application/json" -d '{"body":"race condition test note"}'
}
export -f run_cancel run_note

{
  for _ in $(seq 1 10); do echo "cancel"; done
  for _ in $(seq 1 10); do echo "note"; done
} | xargs -P 20 -I{} bash -c '
    if [ "{}" = "cancel" ]; then run_cancel "'"$COOKIE_JAR"'" "'"$API_BASE"'" "'"$RES_ID"'";
    else run_note "'"$COOKIE_JAR"'" "'"$API_BASE"'" "'"$RES_ID"'"; fi
  ' > /tmp/race_result.txt

echo "응답 분포:"
sort /tmp/race_result.txt | uniq -c

CANCEL_SUCCESS=$(grep -c '^CANCEL:200$' /tmp/race_result.txt || true)
NOTE_SUCCESS=$(grep -c '^NOTE:200$' /tmp/race_result.txt || true)

FINAL_STATUS=$(docker compose exec -T postgres psql -U wonjin -d wonjin -t -A -c \
  "SELECT status FROM wonjin.reservations WHERE id=$RES_ID;" | tr -d '\r')
NOTE_COUNT=$(docker compose exec -T postgres psql -U wonjin -d wonjin -t -A -c \
  "SELECT COUNT(*) FROM wonjin.reservation_notes WHERE reservation_id=$RES_ID;" | tr -d '\r')

echo "취소 성공: $CANCEL_SUCCESS / 상담기록 성공: $NOTE_SUCCESS / 최종상태: $FINAL_STATUS / 실제 저장된 기록: $NOTE_COUNT"

# 불변식: 취소는 최대 1건만 성공(나머지는 409/423 등 거부) + 응답이 200인 상담기록 수만큼
# 정확히 DB에 남아있어야 한다(row-lock이 응답과 실제 저장 결과를 항상 일치시킴).
if [ "$CANCEL_SUCCESS" -eq 1 ] && [ "$FINAL_STATUS" = "Cancelled" ] && [ "$NOTE_SUCCESS" -eq "$NOTE_COUNT" ]; then
  echo "PASS: 취소는 정확히 1건만 성공, 상담기록은 성공 응답 수만큼 정확히 저장됨 — 모순 없음"
else
  echo "FAIL: 취소가 2건 이상 성공했거나, 응답과 실제 저장된 상담기록 수가 어긋남"
  rm -f "$COOKIE_JAR" /tmp/race_result.txt
  exit 1
fi

rm -f "$COOKIE_JAR" /tmp/race_result.txt
