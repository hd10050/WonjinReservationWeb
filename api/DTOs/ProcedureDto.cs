using System.ComponentModel.DataAnnotations;

namespace WonjinApi.DTOs;

// 9-1절 3곳 일치: DB varchar(30)/varchar(50)(AppDbContext.cs Procedure 설정)과 프론트 maxlength가 이 값을 따른다.
// 🔴 D25 — CategoryId 필수(소속 카테고리). sort_order 폐지, 정렬은 이름 오름차순. CategoryId 존재 검증은
// 컨트롤러에서 직접 한다(AssignConsultant의 CONSULTANT_NOT_FOUND와 대칭) — [ApiController] 자동 400은
// 비-nullable int 누락 시 0으로 조용히 바인딩되므로 애노테이션만으론 못 막는다(11-8절 함정).
public record CreateProcedureRequest(
    [Required, MaxLength(30)] string Code,
    [Required, MaxLength(50)] string NameZhCn,
    [Required, MaxLength(50)] string NameZhTw,
    [Required, MaxLength(50)] string NameEn,
    [Required, MaxLength(50)] string NameKo,
    int CategoryId);

public record UpdateProcedureRequest(
    [Required, MaxLength(30)] string Code,
    [Required, MaxLength(50)] string NameZhCn,
    [Required, MaxLength(50)] string NameZhTw,
    [Required, MaxLength(50)] string NameEn,
    [Required, MaxLength(50)] string NameKo,
    int CategoryId,
    bool IsActive);

// 엑셀 일괄등록 — BulkConsultantRequest와 동일한 이유로 DataAnnotations 미부착(ConsultantDto.cs 주석 참고).
// CategoryCode: 소속 카테고리를 코드로 지정(D25) — 서버가 배치 조회 1회로 존재 검증 후 CategoryId로 해석.
public record BulkProcedureRequest(int Row, string? Code, string? NameZhCn, string? NameZhTw, string? NameEn, string? NameKo, string? CategoryCode);
