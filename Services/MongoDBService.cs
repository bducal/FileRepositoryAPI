using FileRepositoryAPI.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using MongoDB.Bson;

namespace FileRepositoryAPI.Services;

public class MongoDBService
{
    private readonly IMongoCollection<FileList> _FileListCollection;
    private readonly MongoClient _MongoClient;
    private readonly string MongoDBName;

    public MongoDBService(IOptions<MongoDBSettings> mongoDBSettings)
    {
        _MongoClient = new MongoClient(mongoDBSettings.Value.ConnectionURI);
        MongoDBName=mongoDBSettings.Value.DatabaseName;
        IMongoDatabase database = _MongoClient.GetDatabase(MongoDBName);
        _FileListCollection = database.GetCollection<FileList>(mongoDBSettings.Value.CollectionName);
    }
    public async Task<List<FileList>> GetAsyncAll()
    {
        return await _FileListCollection.Find(new BsonDocument()).ToListAsync();
    }

    public async Task<byte[]> DownloadFileAsync(string id)
    {
        IGridFSBucket gridFSBucket;
        IMongoDatabase mongoDatabase = _MongoClient.GetDatabase(MongoDBName);

        GridFSBucketOptions imageGridBucketOptions = new GridFSBucketOptions()
        {
            BucketName = "FilesContent",
            ChunkSizeBytes = 1048576,
            ReadPreference = ReadPreference.Secondary,
            WriteConcern = WriteConcern.WMajority
        };

        gridFSBucket = new GridFSBucket(mongoDatabase, imageGridBucketOptions);


        byte[] fileBytes = await gridFSBucket.DownloadAsBytesByNameAsync(id);//.DownloadAsBytesAsync(MongoDB.Bson.ObjectId.Parse(id));
        //using var writer = new BinaryWriter(File.OpenWrite("d:\\temp\\xx.png"));
        //writer.Write(fileBytes);
        return fileBytes;
    }

    public async Task<List<FileList>> UploadFileAsync(IFormFile file, string FileName)
    {
        if (file.Length > 0)
        {
            IGridFSBucket gridFSBucket;
            IMongoDatabase mongoDatabase = _MongoClient.GetDatabase(MongoDBName);

            GridFSBucketOptions imageGridBucketOptions = new GridFSBucketOptions()
            {
                BucketName = "FilesContent",
                ChunkSizeBytes = 1048576,
                ReadPreference = ReadPreference.Secondary,
                WriteConcern = WriteConcern.WMajority
            };

            gridFSBucket = new GridFSBucket(mongoDatabase, imageGridBucketOptions);

            //delete existing
            FilterDefinition<GridFSFileInfo> filter = Builders<GridFSFileInfo>.Filter.Eq(x => x.Filename, FileName);

            //var filter = Builders<GridFSFileInfo>.Filter.Empty;
            var options = new GridFSFindOptions
            {
            };

            using (IAsyncCursor<GridFSFileInfo> cursor = gridFSBucket.Find(filter, options))
            {
                List<GridFSFileInfo> files = cursor.ToList(); // <-- this returns the whole collection

//                var count = files.Count();
                foreach (GridFSFileInfo f in files)
                    gridFSBucket.Delete(f.Id);

            }    
            //end delete existing

            using (var ms = new MemoryStream())
            {
                file.CopyTo(ms);
                var fileBytes = ms.ToArray();
                gridFSBucket.UploadFromBytes(FileName, fileBytes); 
            }
        }

        return await _FileListCollection.Find(new BsonDocument()).ToListAsync();
    }

    public async Task<List<FileList>> GetAsync(string id)
    {
        FilterDefinition<FileList> filter = Builders<FileList>.Filter.Eq("Id", id);
        return await _FileListCollection.Find(filter).ToListAsync();
    }

    public async Task<FileList> CreateAsync(FileList fl)
    {
        await _FileListCollection.InsertOneAsync(fl);
        return fl;
    }

    public async Task<List<FileList>> DeleteAsync(string id)
    {
            IGridFSBucket gridFSBucket;
            IMongoDatabase mongoDatabase = _MongoClient.GetDatabase(MongoDBName);

            GridFSBucketOptions imageGridBucketOptions = new GridFSBucketOptions()
            {
                BucketName = "FilesContent",
                ChunkSizeBytes = 1048576,
                ReadPreference = ReadPreference.Secondary,
                WriteConcern = WriteConcern.WMajority
            };

            gridFSBucket = new GridFSBucket(mongoDatabase, imageGridBucketOptions);

            //delete existing
            FilterDefinition<GridFSFileInfo> filter = Builders<GridFSFileInfo>.Filter.Eq(x => x.Filename, id);

            //var filter = Builders<GridFSFileInfo>.Filter.Empty;
            var options = new GridFSFindOptions
            {
            };

            using (IAsyncCursor<GridFSFileInfo> cursor = gridFSBucket.Find(filter, options))
            {
                List<GridFSFileInfo> files = cursor.ToList(); // <-- this returns the whole collection

                //var count = files.Count();

                foreach (GridFSFileInfo f in files)
                    gridFSBucket.Delete(f.Id);

            }    
            //end delete

        FilterDefinition<FileList> filterDelete = Builders<FileList>.Filter.Eq("Id", id);
        await _FileListCollection.DeleteOneAsync(filterDelete);

        return await _FileListCollection.Find(new BsonDocument()).ToListAsync();
    }

    public async Task<List<FileList>> UpdateAsync(FileList fl)
    {
        FilterDefinition<FileList> filter = Builders<FileList>.Filter.Eq("Id", fl.Id);
        UpdateDefinition<FileList> updateFileTags = Builders<FileList>.Update.Set("FileTags", fl.FileTags);
        UpdateDefinition<FileList> updateFileName = Builders<FileList>.Update.Set("FileName", fl.FileName);
        _FileListCollection.UpdateOne(filter, updateFileTags);
        _FileListCollection.UpdateOne(filter, updateFileName);

        return await _FileListCollection.Find(new BsonDocument()).ToListAsync();
    }
}