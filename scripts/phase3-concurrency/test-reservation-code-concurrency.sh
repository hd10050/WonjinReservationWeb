#!/bin/bash
# F4 — 예약 코드 동시 생성 재현 스크립트 (design.md 8-11절 · 19-1절)
# 8-11절의 원자적 카운터 SQL(INSERT...ON CONFLICT...DO UPDATE...RETURNING)을 같은 날짜로 20회 동시 실행해,
# "MAX(code)+1" 방식이었다면 발생했을 중복 발급이 없는지 검증한다.
# 사용법: 워크트리 루트에서 실행 (docker-compose.override.yml이 같은 디렉터리에 있어야 함)
set -euo pipefail
cd "$(dirname "$0")/../.."

DATE_KEY=$(date +%Y%m%d)-concurtest
docker compose exec -T postgres psql -U wonjin -d wonjin -c \
  "DELETE FROM wonjin.reservation_code_counters WHERE code_date = '2099-01-01';" > /dev/null

run_one() {
  docker compose exec -T postgres psql -U wonjin -d wonjin -t -A -c \
    "INSERT INTO wonjin.reservation_code_counters (code_date, last_seq) VALUES ('2099-01-01', 1)
     ON CONFLICT (code_date) DO UPDATE SET last_seq = wonjin.reservation_code_counters.last_seq + 1
     RETURNING last_seq;"
}
export -f run_one

echo "20건 동시 발급 중..."
# psql 완료 태그("INSERT 0 1")가 -t -A로도 섞여 나올 수 있어 순수 숫자 줄만 결과로 취급한다.
RESULTS=$(seq 1 20 | xargs -P 20 -I{} bash -c run_one | grep -E '^[0-9]+$')

echo "발급된 seq 값:"
echo "$RESULTS" | sort -n

UNIQUE_COUNT=$(echo "$RESULTS" | sort -n | uniq | wc -l)
TOTAL_COUNT=$(echo "$RESULTS" | wc -l)

echo "총 발급: $TOTAL_COUNT / 고유값: $UNIQUE_COUNT"
if [ "$UNIQUE_COUNT" -eq "$TOTAL_COUNT" ] && [ "$TOTAL_COUNT" -eq 20 ]; then
  echo "PASS: 20건 모두 고유한 번호(1~20)를 원자적으로 발급받음 — 중복 없음(F4 통과)"
else
  echo "FAIL: 중복 또는 유실 발생"
  exit 1
fi

docker compose exec -T postgres psql -U wonjin -d wonjin -c \
  "DELETE FROM wonjin.reservation_code_counters WHERE code_date = '2099-01-01';" > /dev/null
