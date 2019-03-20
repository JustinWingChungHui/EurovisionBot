using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace EurovisionCommon
{
    public class EurovisionSongRepository
    {
        public EurovisionSongRepository()
        {
            var authKey = ConfigurationManager.AppSettings["AuthKey"];
            var endpoint = ConfigurationManager.AppSettings["Endpoint"];

            DatabaseId = ConfigurationManager.AppSettings["DatabaseId"];
            CollectionId = ConfigurationManager.AppSettings["CollectionId"];
            client = new DocumentClient(new Uri(endpoint), authKey);
        }

        private readonly DocumentClient client;
        private readonly string DatabaseId;
        private readonly string CollectionId;

        public async Task<Document> UpsertItemAsync(EurovisionSong song)
        {
            var collectionLink = UriFactory.CreateDocumentCollectionUri(DatabaseId, CollectionId);
            return await client.UpsertDocumentAsync(collectionLink, song);
        }

        public async Task<IEnumerable<EurovisionSong>> GetItemsAsync(Expression<Func<EurovisionSong, bool>> predicate, int maxItemCount)
        {
            IDocumentQuery<EurovisionSong> query = client.CreateDocumentQuery<EurovisionSong>(
                UriFactory.CreateDocumentCollectionUri(DatabaseId, CollectionId),
                new FeedOptions { MaxItemCount = maxItemCount, EnableCrossPartitionQuery = true })
                .Where(predicate)
                .AsDocumentQuery();

            List<EurovisionSong> results = new List<EurovisionSong>();
            while (query.HasMoreResults)
            {
                results.AddRange(await query.ExecuteNextAsync<EurovisionSong>());
            }

            return results;
        }
    }
}
