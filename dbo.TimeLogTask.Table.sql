USE [DataWarehouse]
GO
/****** Object:  Table [dbo].[TimeLogTask]    Script Date: 09-12-2015 16:52:52 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[TimeLogTask](
	[ID] [int] NOT NULL,
	[Name] [nvarchar](300) NULL,
	[ProjectID] [int] NULL,
	[Status] [int] NULL,
	[ParentID] [int] NULL,
	[IsParent] [int] NULL,
	[BudgetHours] [decimal](18, 2) NULL,
	[BudgetAmount] [decimal](18, 2) NULL,
	[IsFixedPrice] [int] NULL,
	[StartDate] [date] NULL,
	[EndDate] [date] NULL,
 CONSTRAINT [PK_Task] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
