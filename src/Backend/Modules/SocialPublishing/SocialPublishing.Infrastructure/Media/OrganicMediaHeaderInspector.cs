using System.Buffers.Binary;
using System.Text;
using BuildingBlocks.Domain.Primitives;
using SocialPublishing.Application.Abstractions;
using SocialPublishing.Domain;
using SocialPublishing.Domain.Posts;

namespace SocialPublishing.Infrastructure.Media;

/// <summary>
/// Extrai dimensões e duração a partir dos cabeçalhos JPEG, PNG e MP4.
/// </summary>
public sealed class OrganicMediaHeaderInspector : IOrganicMediaMetadataReader
{
    /// <inheritdoc />
    public Task<Result<OrganicMediaMetadata>> ReadAsync(
        Stream content,
        string mimeType,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (content.CanSeek)
        {
            content.Position = 0;
        }

        return Task.FromResult(ReadInternal(content, mimeType));
    }

    private static Result<OrganicMediaMetadata> ReadInternal(Stream content, string mimeType)
    {
        if (OrganicMediaMimeTypes.IsImage(mimeType))
        {
            return mimeType switch
            {
                OrganicMediaMimeTypes.Jpeg => ReadJpeg(content),
                OrganicMediaMimeTypes.Png => ReadPng(content),
                _ => Result<OrganicMediaMetadata>.Failure(SocialErrors.MediaMimeInvalid)
            };
        }

        if (OrganicMediaMimeTypes.IsVideo(mimeType))
        {
            return ReadMp4(content);
        }

        return Result<OrganicMediaMetadata>.Failure(SocialErrors.MediaMimeInvalid);
    }

    private static Result<OrganicMediaMetadata> ReadPng(Stream content)
    {
        Span<byte> header = stackalloc byte[24];
        if (content.Read(header) < 24)
        {
            return Result<OrganicMediaMetadata>.Failure(SocialErrors.MediaUnreadable);
        }

        if (header[0] != 0x89 || header[1] != (byte)'P' || header[2] != (byte)'N' || header[3] != (byte)'G')
        {
            return Result<OrganicMediaMetadata>.Failure(SocialErrors.MediaUnreadable);
        }

        var width = BinaryPrimitives.ReadInt32BigEndian(header[16..20]);
        var height = BinaryPrimitives.ReadInt32BigEndian(header[20..24]);
        if (width <= 0 || height <= 0)
        {
            return Result<OrganicMediaMetadata>.Failure(SocialErrors.MediaUnreadable);
        }

        return Result<OrganicMediaMetadata>.Success(new OrganicMediaMetadata(width, height, null));
    }

    private static Result<OrganicMediaMetadata> ReadJpeg(Stream content)
    {
        if (content.ReadByte() != 0xFF || content.ReadByte() != 0xD8)
        {
            return Result<OrganicMediaMetadata>.Failure(SocialErrors.MediaUnreadable);
        }

        while (content.Position < content.Length)
        {
            var markerPrefix = content.ReadByte();
            if (markerPrefix < 0)
            {
                break;
            }

            if (markerPrefix != 0xFF)
            {
                continue;
            }

            var marker = content.ReadByte();
            if (marker is 0xC0 or 0xC1 or 0xC2 or 0xC3 or 0xC5 or 0xC6 or 0xC7 or 0xC9 or 0xCA or 0xCB or 0xCD or 0xCE or 0xCF)
            {
                Span<byte> segment = stackalloc byte[7];
                if (content.Read(segment) < 7)
                {
                    return Result<OrganicMediaMetadata>.Failure(SocialErrors.MediaUnreadable);
                }

                var height = BinaryPrimitives.ReadUInt16BigEndian(segment[3..5]);
                var width = BinaryPrimitives.ReadUInt16BigEndian(segment[5..7]);
                if (width == 0 || height == 0)
                {
                    return Result<OrganicMediaMetadata>.Failure(SocialErrors.MediaUnreadable);
                }

                return Result<OrganicMediaMetadata>.Success(new OrganicMediaMetadata(width, height, null));
            }

            var lengthBytes = new byte[2];
            if (content.Read(lengthBytes, 0, 2) < 2)
            {
                break;
            }

            var segmentLength = BinaryPrimitives.ReadUInt16BigEndian(lengthBytes.AsSpan());
            if (segmentLength < 2)
            {
                break;
            }

            content.Position += segmentLength - 2;
        }

        return Result<OrganicMediaMetadata>.Failure(SocialErrors.MediaUnreadable);
    }

    private static Result<OrganicMediaMetadata> ReadMp4(Stream content)
    {
        if (!content.CanSeek)
        {
            return Result<OrganicMediaMetadata>.Failure(SocialErrors.MediaUnreadable);
        }

        var buffer = new byte[Math.Min(content.Length, 8 * 1024 * 1024)];
        content.Position = 0;
        var read = content.Read(buffer, 0, buffer.Length);
        if (read < 32)
        {
            return Result<OrganicMediaMetadata>.Failure(SocialErrors.MediaUnreadable);
        }

        int? durationMs = null;
        int? width = null;
        int? height = null;

        var offset = 0;
        while (offset + 8 <= read)
        {
            var size = BinaryPrimitives.ReadInt32BigEndian(buffer.AsSpan(offset, 4));
            var type = Encoding.ASCII.GetString(buffer, offset + 4, 4);
            if (size < 8)
            {
                break;
            }

            var boxEnd = Math.Min(read, offset + size);
            if (type == "mvhd" && boxEnd - offset >= 32)
            {
                var version = buffer[offset + 8];
                if (version == 0 && boxEnd - offset >= 28)
                {
                    var timescale = BinaryPrimitives.ReadUInt32BigEndian(buffer.AsSpan(offset + 20, 4));
                    var duration = BinaryPrimitives.ReadUInt32BigEndian(buffer.AsSpan(offset + 24, 4));
                    if (timescale > 0 && duration > 0)
                    {
                        durationMs = (int)Math.Min(int.MaxValue, duration * 1000L / timescale);
                    }
                }
            }
            else if (type == "tkhd" && boxEnd - offset >= 84)
            {
                var wFixed = BinaryPrimitives.ReadUInt32BigEndian(buffer.AsSpan(offset + 76, 4));
                var hFixed = BinaryPrimitives.ReadUInt32BigEndian(buffer.AsSpan(offset + 80, 4));
                width = (int)(wFixed / 65536);
                height = (int)(hFixed / 65536);
            }

            offset += size;
        }

        if (width is null or <= 0 || height is null or <= 0 || durationMs is null or <= 0)
        {
            return Result<OrganicMediaMetadata>.Failure(SocialErrors.MediaUnreadable);
        }

        return Result<OrganicMediaMetadata>.Success(new OrganicMediaMetadata(width.Value, height.Value, durationMs));
    }
}
