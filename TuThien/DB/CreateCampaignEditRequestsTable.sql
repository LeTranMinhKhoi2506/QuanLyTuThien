-- T?o b?ng l?u tr? yêu c?u ch?nh s?a chi?n d?ch
CREATE TABLE CampaignEditRequests (
    edit_request_id INT PRIMARY KEY IDENTITY(1,1),
    campaign_id INT NOT NULL,
    requester_id INT NOT NULL,
    
    -- Thông tin thay ??i (JSON format)
    title NVARCHAR(255),
    description NVARCHAR(MAX),
    target_amount DECIMAL(18, 2),
    category_id INT,
    start_date DATETIME2,
    end_date DATETIME2,
    thumbnail_url NVARCHAR(255),
    excess_fund_option VARCHAR(20),
    
    -- Ghi chú v? thay ??i
    change_note NVARCHAR(MAX),
    
    -- Tr?ng thái
    status VARCHAR(20) DEFAULT 'pending' 
        CHECK (status IN ('pending', 'approved', 'rejected')),
    
    -- Admin x? lý
    reviewed_by INT,
    review_note NVARCHAR(MAX),
    reviewed_at DATETIME2,
    
    created_at DATETIME2 DEFAULT GETDATE(),
    
    FOREIGN KEY (campaign_id) REFERENCES Campaigns(campaign_id) ON DELETE CASCADE,
    FOREIGN KEY (requester_id) REFERENCES Users(user_id),
    FOREIGN KEY (reviewed_by) REFERENCES Users(user_id)
);
GO

-- Index ?? tìm ki?m nhanh
CREATE INDEX IX_CampaignEditRequests_CampaignId ON CampaignEditRequests(campaign_id);
CREATE INDEX IX_CampaignEditRequests_Status ON CampaignEditRequests(status);
CREATE INDEX IX_CampaignEditRequests_CreatedAt ON CampaignEditRequests(created_at DESC);
GO
