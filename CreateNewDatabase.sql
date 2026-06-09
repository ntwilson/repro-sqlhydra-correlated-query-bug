/* Used to create the SqlServer database that Schema.fs is generated from */

CREATE TABLE [dbo].[CorrelatedQueryTestTable]
(
  field1 [int] NOT NULL,
  field2 [Date] NOT NULL,
  field3 [float] NOT NULL,
  timestamp [DateTime2](7) NOT NULL,

  CONSTRAINT pk_CorrelatedQueryTestTable PRIMARY KEY (timestamp)
);