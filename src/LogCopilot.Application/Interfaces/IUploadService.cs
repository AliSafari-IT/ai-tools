using LogCopilot.Application.DTOs;

namespace LogCopilot.Application.Interfaces;

public interface IUploadService
{
    Task<UploadSessionDto> CreateSessionAsync(CreateUploadSessionDto dto);
    Task<UploadedFileDto> UploadFileAsync(Guid sessionId, UploadFileDto dto);
    Task StartIngestionAsync(Guid sessionId);
    Task<UploadSessionDto> GetSessionAsync(Guid sessionId);
}
