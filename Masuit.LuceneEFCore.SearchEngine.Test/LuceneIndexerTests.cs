using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Store;
using Masuit.LuceneEFCore.SearchEngine.Test.Helpers;
using Masuit.LuceneEFCore.SearchEngine.Test.Models;
using Microsoft.Extensions.Caching.Memory;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Masuit.LuceneEFCore.SearchEngine.Test
{
    public class LuceneIndexerTests
    {
        private readonly ITestOutputHelper _output;
        private LuceneIndexer _indexer;

        public LuceneIndexerTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void AnIndexCanBeCreated()
        {
            TestDataGenerator tdg = new TestDataGenerator();
            Directory directory = new RAMDirectory();

            Analyzer analyzer = new StandardAnalyzer(Lucene.Net.Util.LuceneVersion.LUCENE_48);
            _indexer = new LuceneIndexer(directory, analyzer);
            _indexer.CreateIndex(tdg.AllData);
            Assert.Equal(2000, _indexer.Count());
            analyzer.Dispose();
            directory.ClearLock("write.lock");
            directory.Dispose();
        }

        [Fact]
        public void AnIndexCanBeDeleted()
        {
            TestDataGenerator tdg = new TestDataGenerator();
            Directory directory = new RAMDirectory();
            Analyzer analyzer = new StandardAnalyzer(Lucene.Net.Util.LuceneVersion.LUCENE_48);
            _indexer = new LuceneIndexer(directory, analyzer);
            _indexer.CreateIndex(tdg.AllData);
            Assert.Equal(2000, _indexer.Count());

            _indexer.DeleteAll();

            Assert.Equal(0, _indexer.Count());
            directory.ClearLock("write.lock");
            analyzer.Dispose();
            directory.Dispose();
        }

        [Fact]
        public void AnItemCanBeAddedToTheIndex()
        {
            TestDataGenerator tdg = new TestDataGenerator();
            Directory directory = new RAMDirectory();
            Analyzer analyzer = new StandardAnalyzer(Lucene.Net.Util.LuceneVersion.LUCENE_48);
            _indexer = new LuceneIndexer(directory, analyzer);
            _indexer.CreateIndex(tdg.AllData);
            Assert.Equal(2000, _indexer.Count());

            _indexer.Add(tdg.ANewUser());

            Assert.Equal(2001, _indexer.Count());
            directory.ClearLock("write.lock");
        }

        [Fact]
        public void AnItemCanBeRemovedFromTheIndex()
        {
            TestDataGenerator tdg = new TestDataGenerator();
            Directory directory = new RAMDirectory();
            Analyzer analyzer = new StandardAnalyzer(Lucene.Net.Util.LuceneVersion.LUCENE_48);
            _indexer = new LuceneIndexer(directory, analyzer);
            _indexer.CreateIndex(tdg.AllData);
            _indexer.Delete(tdg.AllData.First());
            Assert.True(tdg.AllData.Count > _indexer.Count());
            directory.ClearLock("write.lock");
        }

        [Fact]
        public void AnItemCanBeUpdatedInTheIndex()
        {
            TestDataGenerator tdg = new TestDataGenerator();
            Directory directory = new RAMDirectory();
            Analyzer analyzer = new StandardAnalyzer(Lucene.Net.Util.LuceneVersion.LUCENE_48);
            _indexer = new LuceneIndexer(directory, analyzer);
            _indexer.CreateIndex(tdg.AllData);
            LuceneIndexSearcher searcher = new LuceneIndexSearcher(directory, analyzer, new MemoryCache(new MemoryCacheOptions()));

            SearchOptions options = new SearchOptions("ghudson0@rambler.ru", "Email");
            var initialResults = searcher.ScoredSearch(options);
            foreach (var item in initialResults.Results)
            {
                _output.WriteLine($"{item.Score}\t{item.Document.Get("Id")}\t{item.Document.Get("FirstName")}\t{item.Document.Get("Email")}");
            }

            Document rambler = initialResults.Results.First().Document;
            User user = new User()
            {
                Id = int.Parse(rambler.Get("Id")),
                FirstName = rambler.Get("FirstName"),
                Surname = rambler.Get("Surname"),
                Email = rambler.Get("Email"),
                JobTitle = rambler.Get("JobTitle")
            };

            user.FirstName = "Duke";
            user.Surname = "Nukem";
            _indexer.Update(user);
            var endResults = searcher.ScoredSearch(options);
            foreach (var item in endResults.Results)
            {
                _output.WriteLine($"{item.Score}\t{item.Document.Get("Id")}\t{item.Document.Get("FirstName")}\t{item.Document.Get("Email")}");
            }

            Assert.Equal(user.Id.ToString(), endResults.Results.First().Document.Get("Id"));
            Assert.Equal(user.FirstName, endResults.Results.First().Document.Get("FirstName"));
            Assert.Equal(user.Surname, endResults.Results.First().Document.Get("Surname"));
            directory.ClearLock("write.lock");
        }
    }
}