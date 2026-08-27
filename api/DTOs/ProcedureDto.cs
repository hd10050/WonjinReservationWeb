using System.ComponentModel.DataAnnotations;

namespace WonjinApi.DTOs;

// 9-1절 3곳 일치: DB varchar(30)/varchar(50)(AppDbContext.cs Procedure 설정)과 프론트 maxlength가 이 값을 따른다.
public record CreateProcedureRequest(
    [Required, MaxLength(30)] string Code,
    [Required, MaxLength(50)] string NameZhCn,
    [Required, MaxLength(50)] string NameZhTw,
    [Required, MaxLength(50)] string NameEn,
    [Required, MaxLength(50)] string NameKo,
    int SortOrder);

public record UpdateProcedureRequest(
    [Required, MaxLength(30)] string Code,
    [Required, MaxLength(50)] string NameZhCn,
    [Required, MaxLength(50)] string NameZhTw,
    [Required, MaxLength(50)] string NameEn,
    [Required, MaxLength(50)] string NameKo,
    int SortOrder,
    bool IsActive);

// 엑셀 일괄등록 — BulkConsultantRequest와 동일한 이유로 DataAnnotations 미부착(ConsultantDto.cs 주석 참고).
public record BulkProcedureRequest(int Row, string? Code, string? NameZhCn, string? NameZhTw, string? NameEn, string? NameKo, int SortOrder);
