namespace java com.labijie.mozart.test.thrift.idl
namespace csharp Thrift.Samples


enum ResultCode {
  OK, TRY_LATER
}

struct LogEntry {
  1:  string category;
  2:  string message;
}

service Scribe {
  ResultCode Log(1:  list<LogEntry> messages);
  list<LogEntry> getMessages();
}
