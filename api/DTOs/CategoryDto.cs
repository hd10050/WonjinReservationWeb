using System.ComponentModel.DataAnnotations;

namespace WonjinApi.DTOs;

// 9-1절 3곳 일치: DB varchar(30)/varchar(50)(AppDbContext.cs Category 설정)과 프론트 maxlength가 이 값을 따른다.
public record CreateCategoryRequest(
    [Required, MaxLength(30)] string Code,
    [Required, MaxLength(50)] string NameZhCn,
    [Required, MaxLength(50)] string NameZhTw,
    [Required, MaxLength(50)] string NameEn,
    [Required, MaxLength(50)] string NameKo);

public record UpdateCategoryRequest(
    [Required, MaxLength(30)] string Code,
    [Required, MaxLength(50)] string NameZhCn,
    [Required, MaxLength(50)] string NameZhTw,
    [Required, MaxLength(50)] string NameEn,
    [Required, MaxLength(50)] string NameKo,
    bool IsActive);

// 엑셀 일괄등록 — BulkConsultantRequest와 동일한 이유로 DataAnnotations 미부착(ConsultantDto.cs 주석 참고).
public record BulkCategoryRequest(int Row, string? Code, string? NameZhCn, string? NameZhTw, string? NameEn, string? NameKo);

// 카테고리 관리 목록 + 예약 상세 시술 아코디언 그룹 헤더가 공유하는 룩업 DTO.
public record CategoryLookupDto(
    int Id, string Code, string NameZhCn, string NameZhTw, string NameEn, string NameKo, bool IsActive);
