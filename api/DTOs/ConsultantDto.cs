using System.ComponentModel.DataAnnotations;

namespace WonjinApi.DTOs;

// 9-1절 3곳 일치: DB varchar(30)(AppDbContext.cs Consultant.Name HasMaxLength(30))과 프론트 maxlength가 이 값을 따른다.
public record CreateConsultantRequest([Required, MaxLength(30)] string Name, int SortOrder);

public record UpdateConsultantRequest([Required, MaxLength(30)] string Name, int SortOrder, bool IsActive);
