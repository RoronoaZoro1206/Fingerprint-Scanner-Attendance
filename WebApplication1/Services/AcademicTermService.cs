using Fingerprint_Attendance_System.Repositories.Interfaces;

namespace Fingerprint_Attendance_System.Services;

public class AcademicTermService
{
    private readonly IAcademicTermRepository _academicTermRepository;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AcademicTermService(
        IAcademicTermRepository academicTermRepository,
        IHttpContextAccessor httpContextAccessor)
    {
        _academicTermRepository = academicTermRepository;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<string?> GetActiveAcademicTermNameAsync()
    {
        var session = _httpContextAccessor.HttpContext?.Session;
        if (session == null) return null;

        // Try to get from session first
        var termName = session.GetString("ActiveAcademicTermName");
        
        if (string.IsNullOrEmpty(termName))
        {
            // If not in session, fetch from database and store
            await RefreshActiveAcademicTermAsync();
            termName = session.GetString("ActiveAcademicTermName");
        }

        return termName;
    }

    public async Task RefreshActiveAcademicTermAsync()
    {
        var session = _httpContextAccessor.HttpContext?.Session;
        if (session == null) return;

        try
        {
            var allTerms = await _academicTermRepository.GetAllAsync();
            var activeTerm = allTerms.FirstOrDefault(t => t.IsActive);

            if (activeTerm != null)
            {
                session.SetString("ActiveAcademicTermName", activeTerm.TermName);
                session.SetInt32("ActiveAcademicTermId", activeTerm.TermId);
            }
            else
            {
                session.Remove("ActiveAcademicTermName");
                session.Remove("ActiveAcademicTermId");
            }
        }
        catch
        {
            // If error, clear session
            session.Remove("ActiveAcademicTermName");
            session.Remove("ActiveAcademicTermId");
        }
    }

    public void ClearActiveAcademicTerm()
    {
        var session = _httpContextAccessor.HttpContext?.Session;
        if (session == null) return;

        session.Remove("ActiveAcademicTermName");
        session.Remove("ActiveAcademicTermId");
    }
}
