#!/bin/bash
# 소프트 삭제 조건(D15) 동시성 재현 스크립트 (design.md 11-2절 · 19-1절)
# "상담 기록 0건일 때만 삭제 가능"이 2단계(조회->삭제)였다면, 삭제 판단 직후 다른 요청이 상담 기록을
# 추가해도 그 기록째로 예약이 사라질 수 있다. 소프트 삭제(DELETE)와 상담 기록 추가(POST notes)를
# 동시에 쏴서, "삭제됐는데 상담 기록이 남아있는" 모순 상태가 절대 나오지 않는지 검증한다.
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

# 매 실행마다 재현 가능하도록 리셋 — 상담 기록 전부 삭제 + deleted_at 원복
docker compose exec -T postgres psql -U wonjin -d wonjin -c \
  "DELETE FROM wonjin.reservation_notes WHERE reservation_id = $RES_ID;
   UPDATE wonjin.reservations SET deleted_at = NULL, deleted_by_user_id = NULL WHERE id = $RES_ID;" > /dev/null

echo "대상 예약 id=$RES_ID, 소프트삭제 10건 + 상담기록추가 10건 동시 전송..."

run_delete() {
  curl -s -o /dev/null -w "DELETE:%{http_code}\n" -b "$1" -X DELETE "$2/api/admin/reservations/$3"
}
run_note() {
  curl -s -o /dev/null -w "NOTE:%{http_code}\n" -b "$1" -X POST "$2/api/admin/reservations/$3/notes" \
    -H "Content-Type: application/json" -d '{"body":"race condition test note"}'
}
export -f run_delete run_note

{
  for _ in $(seq 1 10); do echo "delete"; done
  for _ in $(seq 1 10); do echo "note"; done
} | xargs -P 20 -I{} bash -c '
    if [ "{}" = "delete" ]; then run_delete "'"$COOKIE_JAR"'" "'"$API_BASE"'" "'"$RES_ID"'";
    else run_note "'"$COOKIE_JAR"'" "'"$API_BASE"'" "'"$RES_ID"'"; fi
  ' > /tmp/race_result.txt

echo "응답 분포:"
sort /tmp/race_result.txt | uniq -c

IS_DELETED=$(docker compose exec -T postgres psql -U wonjin -d wonjin -t -A -c \
  "SELECT deleted_at IS NOT NULL FROM wonjin.reservations WHERE id=$RES_ID;" | tr -d '\r')
NOTE_COUNT=$(docker compose exec -T postgres psql -U wonjin -d wonjin -t -A -c \
  "SELECT COUNT(*) FROM wonjin.reservation_notes WHERE reservation_id=$RES_ID;" | tr -d '\r')

echo "최종 deleted=$IS_DELETED, 상담기록 건수=$NOTE_COUNT"

# 불변식: 삭제됐다면 반드시 상담기록 0건, 상담기록이 있다면 반드시 미삭제 상태여야 한다.
if [ "$IS_DELETED" = "t" ] && [ "$NOTE_COUNT" -eq 0 ]; then
  echo "PASS: 삭제가 이겼고 상담 기록 0건 유지 — 모순 없음"
elif [ "$IS_DELETED" = "f" ] && [ "$NOTE_COUNT" -gt 0 ]; then
  echo "PASS: 상담 기록 추가가 이겨 삭제가 전부 거부됨 — 모순 없음"
else
  echo "FAIL: 삭제됐는데 상담 기록이 남아있는 모순 상태(또는 그 반대) 발생"
  rm -f "$COOKIE_JAR" /tmp/race_result.txt
  exit 1
fi

rm -f "$COOKIE_JAR" /tmp/race_result.txt
