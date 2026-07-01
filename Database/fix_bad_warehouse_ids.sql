-- ============================================================
-- fix_bad_warehouse_ids.sql
-- Run ONCE to correct WarehouseItemID values that were inserted
-- with wrong IDs (WI-0025, WI-0026) instead of the correct
-- WHI-R-00xx format defined in sample_data.sql.
--
-- MRQ-260701-001-01  WI-0026  → should be WHI-R-0002  (IID-R-0002 High-density Foam)
-- MRQ-260701-001-02  WI-0025  → should be WHI-R-0001  (IID-R-0001 Solid Oak Panel)
-- ============================================================

UPDATE MaterialRequest
   SET WarehouseItemID = 'WHI-R-0002'
 WHERE RequestID = 'MRQ-260701-001-01'
   AND WarehouseItemID = 'WI-0026';

UPDATE MaterialRequest
   SET WarehouseItemID = 'WHI-R-0001'
 WHERE RequestID = 'MRQ-260701-001-02'
   AND WarehouseItemID = 'WI-0025';

-- Verify
SELECT RequestID, RawMaterialItemID, WarehouseItemID
FROM   MaterialRequest
WHERE  RequestID LIKE 'MRQ-260701-001%'
ORDER  BY RequestID;
