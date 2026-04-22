-- =============================================================
-- Audit Log + custom_fields extensions for Northwind CRM MCP
-- =============================================================

-- Audit log: links every Claude action to the DB change it caused
-- via correlation_id that flows from MCP Server → API → DB

CREATE TABLE IF NOT EXISTS audit_log (
    id              BIGSERIAL PRIMARY KEY,
    correlation_id  UUID        NOT NULL,
    user_prompt     TEXT,                   -- original natural language request
    tool_name       TEXT,                   -- MCP tool that was called
    tool_input      JSONB,                  -- parameters Claude passed to the tool
    entity_type     TEXT,                   -- e.g. 'order', 'product', 'customer'
    entity_id       TEXT,
    action          TEXT        NOT NULL,   -- INSERT / UPDATE / DELETE / SELECT
    old_values      JSONB,
    new_values      JSONB,
    performed_by    TEXT,                   -- Auth0 user sub or service account
    performed_at    TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_audit_correlation  ON audit_log(correlation_id);
CREATE INDEX IF NOT EXISTS idx_audit_entity       ON audit_log(entity_type, entity_id);
CREATE INDEX IF NOT EXISTS idx_audit_performed_at ON audit_log(performed_at DESC);
CREATE INDEX IF NOT EXISTS idx_audit_tool         ON audit_log(tool_name);

-- =============================================================
-- JSONB custom_fields on key Northwind tables
-- Mirrors the pattern used in Centerfield CRM for semi-structured data
-- =============================================================

ALTER TABLE customers  ADD COLUMN IF NOT EXISTS custom_fields JSONB DEFAULT '{}';
ALTER TABLE products   ADD COLUMN IF NOT EXISTS custom_fields JSONB DEFAULT '{}';
ALTER TABLE orders     ADD COLUMN IF NOT EXISTS custom_fields JSONB DEFAULT '{}';
ALTER TABLE employees  ADD COLUMN IF NOT EXISTS custom_fields JSONB DEFAULT '{}';

-- GIN indexes for efficient JSONB querying
CREATE INDEX IF NOT EXISTS idx_customers_custom ON customers USING GIN(custom_fields);
CREATE INDEX IF NOT EXISTS idx_products_custom  ON products  USING GIN(custom_fields);

-- =============================================================
-- Example: seed some custom_fields data for demo purposes
-- =============================================================

UPDATE customers
SET custom_fields = '{"tier": "gold", "preferred_contact": "email", "notes": "Key account"}'
WHERE customer_id = 'ALFKI';

UPDATE products
SET custom_fields = '{"warehouse_location": "A3", "reorder_notes": "Long lead time"}'
WHERE product_id = 1;
