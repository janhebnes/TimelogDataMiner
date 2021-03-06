USE [DataWarehouse]
GO
/****** Object:  Table [dbo].[TimeLogAllocation]    Script Date: 09-12-2015 16:52:52 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[TimeLogAllocation](
	[ID] [int] NOT NULL,
	[ProjectID] [int] NULL,
	[TaskID] [int] NULL,
	[EmployeeID] [int] NULL,
	[AllocatedHours] [decimal](18, 2) NULL,
	[HourlyRate] [decimal](18, 2) NULL,
	[TaskIsFixedPrice] [int] NULL,
 CONSTRAINT [PK_Allocation] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
