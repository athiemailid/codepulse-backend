using AutoMapper;
using CodePulseApi.DTOs;
using CodePulseApi.Models;

namespace CodePulseApi.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Repository mappings
        CreateMap<Repository, RepositoryDto>()
            .ForMember(dest => dest.TotalPullRequests, opt => opt.MapFrom(src => src.PullRequests.Count))
            .ForMember(dest => dest.TotalCommits, opt => opt.MapFrom(src => src.Commits.Count));
            
        CreateMap<Repository, RepositoryResponseDto>()
            .ForMember(dest => dest.TotalPullRequests, opt => opt.MapFrom(src => src.PullRequests.Count))
            .ForMember(dest => dest.TotalCommits, opt => opt.MapFrom(src => src.Commits.Count));
        
        CreateMap<CreateRepositoryDto, Repository>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.PullRequests, opt => opt.Ignore())
            .ForMember(dest => dest.Commits, opt => opt.Ignore());

        // Engineer mappings
        CreateMap<Engineer, EngineerDto>()
            .ForMember(dest => dest.TotalCommits, opt => opt.MapFrom(src => src.Commits.Count))
            .ForMember(dest => dest.TotalPullRequests, opt => opt.MapFrom(src => src.PullRequests.Count));
            
        CreateMap<Engineer, EngineerResponseDto>();

        // Pull Request mappings
        CreateMap<PullRequest, PullRequestDto>()
            .ForMember(dest => dest.RepositoryName, opt => opt.MapFrom(src => src.Repository.Name))
            .ForMember(dest => dest.CreatedByName, opt => opt.MapFrom(src => src.CreatedBy.Name))
            .ForMember(dest => dest.TotalReviews, opt => opt.MapFrom(src => src.Reviews.Count))
            .ForMember(dest => dest.AverageReviewScore, opt => opt.MapFrom(src => 
                src.Reviews.Any() ? src.Reviews.Average(r => r.Score) : 0.0));
                
        CreateMap<PullRequest, PullRequestResponseDto>()
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedDate))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.CreatedDate))
            .ForMember(dest => dest.SourceBranch, opt => opt.MapFrom(src => src.SourceRefName))
            .ForMember(dest => dest.TargetBranch, opt => opt.MapFrom(src => src.TargetRefName));

        // Commit mappings
        CreateMap<Commit, CommitDto>()
            .ForMember(dest => dest.RepositoryName, opt => opt.MapFrom(src => src.Repository != null ? src.Repository.Name : ""))
            .ForMember(dest => dest.AuthorName, opt => opt.MapFrom(src => src.Author));
            
        CreateMap<Commit, CommitResponseDto>()
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CommitDate))
            .ForMember(dest => dest.CommitHash, opt => opt.MapFrom(src => src.CommitId));

        // Review mappings
        CreateMap<Review, ReviewDto>()
            .ForMember(dest => dest.PullRequestTitle, opt => opt.MapFrom(src => src.PullRequest != null ? src.PullRequest.Title : ""))
            .ForMember(dest => dest.RepositoryName, opt => opt.MapFrom(src => src.PullRequest != null ? src.PullRequest.Repository.Name : ""))
            .ForMember(dest => dest.EngineerName, opt => opt.MapFrom(src => src.PullRequest != null ? src.PullRequest.CreatedBy.Name : ""));
            
        CreateMap<Review, ReviewResponseDto>();

        CreateMap<CreateReviewDto, Review>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.PullRequest, opt => opt.Ignore());

        // Webhook Event mappings
        CreateMap<WebhookEvent, WebhookEventDto>();

        // Pull Request Summary for AI
        CreateMap<PullRequest, PullRequestSummaryDto>()
            .ForMember(dest => dest.Commits, opt => opt.MapFrom(src => src.Commits.Select(c => new CommitSummaryDto
            {
                Message = c.Message,
                Author = c.Author,
                ChangedFiles = c.ChangedFiles
            })));
    }
}
