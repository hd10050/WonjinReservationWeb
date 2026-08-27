using System.ComponentModel.DataAnnotations;

namespace WonjinApi.DTOs;

// 9-1절 3곳 일치: DB varchar(30)(AppDbContext.cs Consultant.Name HasMaxLength(30))과 프론트 maxlength가 이 값을 따른다.
public record CreateConsultantRequest([Required, MaxLength(30)] string Name, int SortOrder);

public record UpdateConsultantRequest([Required, MaxLength(30)] string Name, int SortOrder, bool IsActive);

// 엑셀 일괄등록 — excel-bulk-upload-pattern-reference.md 레이어3(백엔드 권위 검증) 패턴.
// Row: 프론트가 계산한 실제 엑셀 행 번호(헤더=1행)를 그대로 실어 보낸다(서버 재계산 금지 — 메시지 어긋남 방지).
// DataAnnotations를 안 붙인다 — [ApiController] 자동 400은 첫 오류에서 멈추므로, 전체 행 오류를
// 모아서 반환하려면 컨트롤러에서 직접 순회 검증해야 한다.
public record BulkConsultantRequest(int Row, string? Name, int SortOrder);

// consultants·procedures 벌크 엔드포인트가 공유하는 행별 오류 표현.
// Field는 프론트가 어떤 필드였는지 로컬 라벨로 번역하는 데 쓰는 키일 뿐, 문장 자체는 아니다(4-로케일 원칙 준수).
public record BulkRowError(int Row, string Code, string? Field = null, int? Length = null, int? Max = null);
