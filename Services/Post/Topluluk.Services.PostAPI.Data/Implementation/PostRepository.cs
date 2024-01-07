using System;
using System.Linq.Expressions;
using DBHelper.Connection;
using DBHelper.Repository.Mongo;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Topluluk.Services.PostAPI.Data.Interface;
using Topluluk.Services.PostAPI.Model.Dto;
using Topluluk.Services.PostAPI.Model.Entity;
using Topluluk.Shared.Dtos;
using Topluluk.Shared.Messages.User;

namespace Topluluk.Services.PostAPI.Data.Implementation
{
    public class PostRepository : MongoGenericRepository<Post>, IPostRepository
	{

        private readonly IConnectionFactory _connectionFactory;
        private readonly IMongoClient _mongoClient;

        public PostRepository(IConnectionFactory connectionFactory, IMongoClient mongoClient) : base(connectionFactory)
        {
            _connectionFactory = connectionFactory;
            _mongoClient = mongoClient;
        }
        private new IMongoDatabase GetConnection() => (MongoDB.Driver.IMongoDatabase)_connectionFactory.GetConnection;

        private string GetCollectionName() => $"{nameof(Post)}Collection";


        public async Task<GetPostByIdDto> GetPostById(string id)
        {
            var database = GetConnection();
            var collectionName = GetCollectionName();


            var lookupStage = new BsonDocument("$lookup", new BsonDocument
            {
                { "from", "PostInteractionCollection" },
                { "localField", "_id" },
                { "foreignField", "PostId" },
                { "as", "Interactions" }
            });

            var addInteractionCoundFieldsStage = new BsonDocument("$addFields", new BsonDocument
                {
                    { "InteractionCount", new BsonDocument("$size", "$Interactions") }
                });

            var projectInteractionsStage = new BsonDocument("$project", new BsonDocument
                {
                    { "Interactions", 0 }
                }
            );

            var lookupCommentsStage = new BsonDocument("$lookup", new BsonDocument
            {
                { "from", "PostCommentCollection" },
                { "let", new BsonDocument("postId", "$_id") },
                { "pipeline", new BsonArray
                    {
                        new BsonDocument("$match", new BsonDocument
                        {
                            { "$expr", new BsonDocument("$and", new BsonArray
                                {
                                    new BsonDocument("$eq", new BsonArray { "$PostId", "$$postId" }),
                                    new BsonDocument("$eq", new BsonArray { "$IsDeleted", false }) // Eklenen satır
                                }
                            )}
                        }),
                        new BsonDocument("$skip", 0),
                        new BsonDocument("$limit", 10)
                    }
                },
                { "as", "Comments" }
            });


            var addCommentCountFieldsStage = new BsonDocument("$addFields", new BsonDocument
            {
                    { "CommentCount", new BsonDocument("$size", "$Comments") }
            });

            var filter = Builders<Post>.Filter.Where(p =>  p.Id == id);
            var sort = Builders<Post>.Sort.Descending(p => p.CreatedAt);

            var cursor = await database.GetCollection<Post>(collectionName)
                                        .Aggregate()
                                        .Match(filter)
                                        .AppendStage<BsonDocument>(lookupStage)
                                        .AppendStage<BsonDocument>(addInteractionCoundFieldsStage)
                                        .AppendStage<BsonDocument>(projectInteractionsStage)
                                        .AppendStage<BsonDocument>(lookupCommentsStage)
                                        .AppendStage<BsonDocument>(addCommentCountFieldsStage)
                                        .ToCursorAsync();

            var document = await cursor.FirstOrDefaultAsync();

            return BsonSerializer.Deserialize<GetPostByIdDto>(document);
        }

        public async Task<bool> DeletePosts(string userId)
        {

            var database = GetConnection();
            var collectionName = GetCollectionName();

            var filter = Builders<Post>.Filter.And(
                Builders<Post>.Filter.Eq(p => p.User.Id, userId),
                Builders<Post>.Filter.Eq(p => p.IsDeleted, false));

            var update = Builders<Post>.Update.Set(p => p.IsDeleted, true);

            var result = database.GetCollection<Post>(collectionName).UpdateMany(filter, update);
            return await Task.FromResult(result.ModifiedCount > 0);
        }

        /// <summary>
        /// Get posts descending by create date
        /// </summary>
        /// <param name="skip"></param>
        /// <param name="take"></param>
        /// <param name="expression"></param>
        /// <returns></returns>
        public async Task<List<GetPostForFeedDto>> GetPostsWithDescending(string sourceUserId, int skip, int take, Expression<Func<Post, bool>> expression)
        {
            var database = GetConnection();
            var collectionName = GetCollectionName();

            var postInteractionCollection = database.GetCollection<PostInteraction>("PostInteractionCollection");
            var postCommentCollection = database.GetCollection<PostComment>("PostCommentCollection");

            var lookupStage = new BsonDocument("$lookup", new BsonDocument
            {
                { "from", "PostInteractionCollection" },
                { "let", new BsonDocument("postId", "$_id") },
                { "pipeline", new BsonArray 
                    { 
                        new BsonDocument("$match", new BsonDocument
                        {
                            { "$expr", new BsonDocument("$and", new BsonArray
                                {
                                    new BsonDocument("$eq", new BsonArray { "$PostId", "$$postId" }),
                                    new BsonDocument("$eq", new BsonArray { "$IsDeleted", false }) 
                                })    
                            }
                        })    
                    }
                },
                { "as", "Interactions" }
            });
          
            var addInteractionCoundFieldsStage = new BsonDocument("$addFields", new BsonDocument
                {
                    { "InteractionCount", new BsonDocument("$size", "$Interactions") }
                });

            var projectInteractionsStage = new BsonDocument("$project", new BsonDocument
                {
                    { "Interactions", 0 }
                }
            );
             
            var lookupCommentsStage = new BsonDocument("$lookup", new BsonDocument
            {
                { "from", "PostCommentCollection" },
                { "let", new BsonDocument("postId", "$_id") },
                { "pipeline", new BsonArray
                    {
                        new BsonDocument("$match", new BsonDocument
                        {
                            { "$expr", new BsonDocument("$and", new BsonArray
                                {
                                    new BsonDocument("$eq", new BsonArray { "$PostId", "$$postId" }),
                                    new BsonDocument("$eq", new BsonArray { "$IsDeleted", false })
                                })
                            }
                        })
                    }
                },
                { "as", "Comments" }
            });

            var addCommentCountFieldsStage = new BsonDocument("$addFields", new BsonDocument
            {
                    { "CommentCount", new BsonDocument("$size", "$Comments") }
            });
            var projectCommentsStage = new BsonDocument("$project", new BsonDocument
            {
                { "Comments", 0 }
            });


            var lookupIsSavedStage = new BsonDocument("$lookup", new BsonDocument
            {
                { "from", "SavedPostCollection" },
                { "let", new BsonDocument("postId", "$_id") },
                { "pipeline", new BsonArray
                    {
                        new BsonDocument("$match", new BsonDocument
                        {
                            { "$expr", new BsonDocument("$and", new BsonArray
                                {
                                    new BsonDocument("$eq", new BsonArray { "$PostId", "$$postId" }),
                                    new BsonDocument("$eq", new BsonArray { "$IsDeleted", false }),
                                    new BsonDocument("$eq", new BsonArray { "$User._id", sourceUserId })
                                })
                            }
                        }),
                        new BsonDocument("$limit", 1)
                    }
                },
                { "as", "Saveds" }
            });

            var addIsSavedFieldsStage = new BsonDocument("$addFields", new BsonDocument
            {
                { "IsSaved", new BsonDocument("$cond", new BsonArray
                    {
                        new BsonDocument("$eq", new BsonArray { new BsonDocument("$size", "$Saveds"), 1 }), // $Saveds dizisinin boyutunu kontrol et
                        true,
                        false
                    })
                }
            });
            var filter = Builders<Post>.Filter.Where(expression);
            var sort = Builders<Post>.Sort.Descending(p => p.CreatedAt);

            var cursor = await database.GetCollection<Post>(collectionName)
                                        .Aggregate()
                                        .Match(filter)
                                        .Sort(sort)
                                        .Skip(skip * take)
                                        .Limit(take)
                                        .AppendStage<BsonDocument>(lookupStage)
                                        .AppendStage<BsonDocument>(addInteractionCoundFieldsStage)
                                        .AppendStage<BsonDocument>(projectInteractionsStage)
                                        .AppendStage<BsonDocument>(lookupCommentsStage)
                                        .AppendStage<BsonDocument>(addCommentCountFieldsStage)
                                        .AppendStage<BsonDocument>(projectCommentsStage)
                                        .AppendStage<BsonDocument>(lookupIsSavedStage)
                                        .AppendStage<BsonDocument>(addIsSavedFieldsStage)
                                        .ToCursorAsync();

            var documents = await cursor.ToListAsync();
            var posts = new List<GetPostForFeedDto>();

            foreach (var document in documents)
            {
                var post = BsonSerializer.Deserialize<GetPostForFeedDto>(document);
                posts.Add(post);
            }
            return posts;
        }

        public async Task<bool> UserUpdated( User newUser)
        {
            var database = GetConnection();
            var collectionName = GetCollectionName();
            using (var session = await _mongoClient.StartSessionAsync())
            {
                session.StartTransaction();
                try
                {
                    long documentCount = await database.GetCollection<Post>(collectionName).CountDocumentsAsync(session, p => p.User.Id == newUser.Id);
                    var response = await database.GetCollection<Post>(collectionName).UpdateManyAsync(session, p => p.User.Id == newUser.Id, Builders<Post>.Update.Set(p => p.User, newUser));
                    
                    if(response.ModifiedCount == documentCount)
                    {
                        await session.CommitTransactionAsync();
                        return true;
                    }
                    else
                    {
                        throw new Exception($"Modified counts doesnt match, documentCount: {documentCount}, modifiedCount: {response.ModifiedCount}");
                    }
                }
                catch(Exception e)
                {
                    await session.AbortTransactionAsync();
                    return false;
                }
            }
              
        }


    }
}

